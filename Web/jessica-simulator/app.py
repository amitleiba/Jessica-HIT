"""
Jessica Simulator — fake robot WebSocket server for local development.

JessicaManager connects as a WebSocket *client* to JessicaWebSocket:Url.
This process accepts that connection and speaks the same JSON protocol:

  Inbound (from JessicaManager → "robot"):
    {"cmd":"move","left":<int>,"right":<int>}
    {"cmd":"stop"}

  Outbound (robot → JessicaManager):
    {"type":"telemetry","distance":<int>,"safety":<int>,"mode":<int>,"battery":<float>}

Point JessicaManager appsettings.json at this server, for example:
  "JessicaWebSocket": { "Url": "ws://127.0.0.1:8765" }

Then start/stop/direction from your app will hit JessicaManager, which forwards
move/stop here; each command triggers a fresh random telemetry frame (and
optional periodic telemetry for live UI polling).
"""

from __future__ import annotations

import argparse
import asyncio
import contextlib
import json
import logging
import random
from typing import Any

import websockets

LOG = logging.getLogger("jessica-simulator")


def random_telemetry() -> dict[str, Any]:
    return {
        "type": "telemetry",
        "distance": random.randint(0, 999),
        "safety": random.randint(0, 2),
        "mode": random.randint(0, 3),
        "battery": round(random.uniform(3.0, 4.2), 2),
    }


async def send_telemetry(ws: Any, reason: str) -> None:
    payload = random_telemetry()
    text = json.dumps(payload, separators=(",", ":"))
    await ws.send(text)
    LOG.info(
        "telemetry reason=%s distance=%s safety=%s mode=%s battery=%s",
        reason,
        payload["distance"],
        payload["safety"],
        payload["mode"],
        payload["battery"],
    )


async def telemetry_loop(ws: Any, interval_s: float, stop: asyncio.Event) -> None:
    if interval_s <= 0:
        return
    try:
        while not stop.is_set():
            await asyncio.sleep(interval_s)
            if stop.is_set():
                break
            await send_telemetry(ws, "interval")
    except asyncio.CancelledError:
        return
    except websockets.exceptions.ConnectionClosed:
        LOG.info("telemetry loop ended (connection closed)")


async def handle_client(ws: Any, telemetry_interval_s: float) -> None:
    peer = ws.remote_address
    LOG.info("client connected from %s", peer)

    stop = asyncio.Event()
    ticker: asyncio.Task[None] | None = None

    try:
        await send_telemetry(ws, "hello")
        if telemetry_interval_s > 0:
            ticker = asyncio.create_task(telemetry_loop(ws, telemetry_interval_s, stop))

        async for message in ws:
            if not isinstance(message, str):
                LOG.warning("ignoring non-text frame from %s", peer)
                continue

            raw = message.strip()
            LOG.debug("recv %s", raw)

            try:
                data = json.loads(raw)
            except json.JSONDecodeError:
                LOG.warning("invalid json from %s: %s", peer, raw[:200])
                continue

            cmd = data.get("cmd")
            if cmd == "move":
                LOG.info(
                    "command move left=%s right=%s (raw=%s)",
                    data.get("left"),
                    data.get("right"),
                    raw,
                )
                await send_telemetry(ws, "after_move")
            elif cmd == "stop":
                LOG.info("command stop (raw=%s)", raw)
                await send_telemetry(ws, "after_stop")
            else:
                LOG.info("unknown message cmd=%r data=%s", cmd, data)
                await send_telemetry(ws, "after_unknown")

    except websockets.exceptions.ConnectionClosedOK:
        LOG.info("client disconnected cleanly from %s", peer)
    except websockets.exceptions.ConnectionClosed as ex:
        LOG.info("client disconnected from %s code=%s reason=%s", peer, ex.code, ex.reason)
    finally:
        stop.set()
        if ticker is not None:
            ticker.cancel()
            with contextlib.suppress(asyncio.CancelledError):
                await ticker


def parse_args() -> argparse.Namespace:
    p = argparse.ArgumentParser(description="Jessica robot WebSocket simulator")
    p.add_argument("--host", default="127.0.0.1", help="Bind address")
    p.add_argument(
        "--port",
        type=int,
        default=8765,
        help="Bind port (set JessicaWebSocket:Url to ws://127.0.0.1:<port>)",
    )
    p.add_argument(
        "--telemetry-interval",
        type=float,
        default=1.0,
        metavar="SEC",
        help="Send random telemetry every SEC seconds (0 = only on commands)",
    )
    p.add_argument(
        "-v",
        "--verbose",
        action="store_true",
        help="Debug log including raw traffic",
    )
    return p.parse_args()


def main() -> None:
    args = parse_args()
    logging.basicConfig(
        level=logging.DEBUG if args.verbose else logging.INFO,
        format="%(asctime)s %(levelname)s %(message)s",
    )

    async def _bound_handler(ws: Any) -> None:
        await handle_client(ws, args.telemetry_interval)

    async def run() -> None:
        async with websockets.serve(_bound_handler, args.host, args.port):
            LOG.info(
                "listening ws://%s:%s (telemetry_interval=%ss)",
                args.host,
                args.port,
                args.telemetry_interval,
            )
            await asyncio.Future()

    try:
        asyncio.run(run())
    except KeyboardInterrupt:
        LOG.info("shutdown")


if __name__ == "__main__":
    main()
