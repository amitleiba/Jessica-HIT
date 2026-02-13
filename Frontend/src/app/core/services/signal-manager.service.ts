import { Injectable } from '@angular/core';
import { Subject, Observable, filter, map, BehaviorSubject } from 'rxjs';
import {
    HubConnection,
    HubConnectionBuilder,
    HubConnectionState,
    LogLevel,
    HttpTransportType,
} from '@microsoft/signalr';
import { environment } from '../../../environments/environment';

/**
 * Inbound message wrapper — used by the internal `on()` stream.
 * Hub server-to-client invocations are normalised into this shape.
 */
export interface SignalMessage<T = any> {
    event: string;
    data: T;
}

export type SignalConnectionState = 'disconnected' | 'connecting' | 'connected' | 'reconnecting' | 'error';

/**
 * SignalManagerService — centralised **SignalR** gateway for the entire app.
 *
 * Wraps the @microsoft/signalr HubConnection behind a simple API:
 *   send(method, data)        → invokes a hub method on the server
 *   on<T>(method)             → returns an Observable of server-to-client calls
 *   connectionState$          → current connection lifecycle state
 *
 * Authentication:
 *   Passes the JWT stored in localStorage via `accessTokenFactory` so the
 *   [Authorize] hub receives a valid Bearer token on every request.
 */
@Injectable({ providedIn: 'root' })
export class SignalManagerService {
    // ── Observables ──
    private messageSubject = new Subject<SignalMessage>();
    private connectionState = new BehaviorSubject<SignalConnectionState>('disconnected');

    /** Raw stream of every inbound hub invocation (normalised) */
    messages$ = this.messageSubject.asObservable();

    /** Current connection lifecycle state */
    connectionState$ = this.connectionState.asObservable();

    // ── Internals ──
    private connection: HubConnection | null = null;

    /** Track which server→client methods we've already registered listeners for */
    private registeredListeners = new Set<string>();

    // ─────────────────────────────────────────────
    //  Connect / Disconnect
    // ─────────────────────────────────────────────

    /**
     * Build and start the SignalR hub connection.
     * @param url  Override URL; defaults to `environment.signalRUrl`
     */
    connect(url?: string): void {
        if (
            this.connection &&
            (this.connection.state === HubConnectionState.Connected ||
                this.connection.state === HubConnectionState.Connecting ||
                this.connection.state === HubConnectionState.Reconnecting)
        ) {
            console.warn('[SignalManager] Already connected or connecting — skipping.');
            return;
        }

        const hubUrl = url ?? environment.signalRUrl;
        this.connectionState.next('connecting');
        console.log(`[SignalManager] Connecting to ${hubUrl} …`);

        this.connection = new HubConnectionBuilder()
            .withUrl(hubUrl, {
                accessTokenFactory: () => localStorage.getItem('access_token') ?? '',
                transport: HttpTransportType.WebSockets | HttpTransportType.LongPolling,
            })
            .withAutomaticReconnect([0, 1000, 2000, 5000, 10000, 30000]) // retry delays
            .configureLogging(environment.production ? LogLevel.Warning : LogLevel.Information)
            .build();

        // ── Lifecycle hooks ──

        this.connection.onreconnecting((error) => {
            console.warn('[SignalManager] 🔄 Reconnecting …', error?.message);
            this.connectionState.next('reconnecting');
        });

        this.connection.onreconnected((connectionId) => {
            console.log('[SignalManager] ✅ Reconnected, connectionId:', connectionId);
            this.connectionState.next('connected');
        });

        this.connection.onclose((error) => {
            console.log('[SignalManager] Connection closed', error?.message);
            this.connectionState.next('disconnected');
        });

        // ── Start ──

        this.connection
            .start()
            .then(() => {
                console.log('[SignalManager] ✅ Connected');
                this.connectionState.next('connected');
            })
            .catch((err) => {
                console.error('[SignalManager] ❌ Connection failed', err);
                this.connectionState.next('error');
            });
    }

    /**
     * Gracefully stop the connection.
     */
    disconnect(): void {
        if (this.connection) {
            this.connection.stop();
            this.connection = null;
            this.registeredListeners.clear();
        }
        this.connectionState.next('disconnected');
        console.log('[SignalManager] Disconnected (manual)');
    }

    // ─────────────────────────────────────────────
    //  Send  (client → server hub method)
    // ─────────────────────────────────────────────

    /**
     * Invoke a hub method on the server.
     * Uses `send` (fire-and-forget) — switch to `invoke` if you need a return value.
     *
     * @param method  The hub method name (PascalCase), e.g. "CarDirectionChange"
     * @param data    The payload object passed as the method argument
     */
    send<T = any>(method: string, data: T): void {
        if (!this.connection || this.connection.state !== HubConnectionState.Connected) {
            console.warn(`[SignalManager] Cannot send "${method}" — not connected.`);
            return;
        }

        console.log(`[SignalManager] ➡ Invoking: ${method}`, data);
        this.connection.send(method, data).catch((err) => {
            console.error(`[SignalManager] Failed to send "${method}"`, err);
        });
    }

    /**
     * Invoke a hub method and wait for the server's return value.
     */
    invoke<TResult = void, TData = any>(method: string, data?: TData): Promise<TResult> {
        if (!this.connection || this.connection.state !== HubConnectionState.Connected) {
            return Promise.reject(new Error(`Cannot invoke "${method}" — not connected.`));
        }

        console.log(`[SignalManager] ➡ Invoking (with result): ${method}`, data);
        return data !== undefined
            ? this.connection.invoke<TResult>(method, data)
            : this.connection.invoke<TResult>(method);
    }

    // ─────────────────────────────────────────────
    //  Receive  (server → client)
    // ─────────────────────────────────────────────

    /**
     * Subscribe to a server-to-client hub method.
     * Returns an Observable that emits the payload each time the server calls this method.
     *
     * Listeners are registered lazily and only once per method name.
     *
     * Example:
     *   this.signalManager.on<VideoFrame>('ReceiveVideoFrame').subscribe(frame => render(frame));
     */
    on<T = any>(method: string): Observable<T> {
        this.ensureListener(method);

        return this.messages$.pipe(
            filter((msg) => msg.event === method),
            map((msg) => msg.data as T)
        );
    }

    /**
     * Whether the hub is currently connected and ready.
     */
    get isConnected(): boolean {
        return this.connection?.state === HubConnectionState.Connected;
    }

    // ─────────────────────────────────────────────
    //  Internal helpers
    // ─────────────────────────────────────────────

    /**
     * Register a SignalR `.on()` listener for a method name (only once).
     * Every invocation from the server is piped into `messageSubject`.
     */
    private ensureListener(method: string): void {
        if (this.registeredListeners.has(method) || !this.connection) return;

        this.connection.on(method, (...args: any[]) => {
            const data = args.length === 1 ? args[0] : args;
            console.log(`[SignalManager] ⬅ Received: ${method}`, data);
            this.messageSubject.next({ event: method, data });
        });

        this.registeredListeners.add(method);
    }
}

