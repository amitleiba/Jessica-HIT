import { Injectable, signal } from "@angular/core";

export type ThemeMode = "light" | "dark";

@Injectable({
  providedIn: "root",
})
export class ThemeService {
  private readonly THEME_KEY = "app-theme";
  private readonly DARK_CLASS = "app-dark-mode";

  currentTheme = signal<ThemeMode>(
    (localStorage.getItem(this.THEME_KEY) as ThemeMode) || "dark"
  );

  constructor() {
    this.applyTheme(this.currentTheme());
  }

  toggleTheme(): void {
    const newTheme = this.currentTheme() === "light" ? "dark" : "light";
    this.setTheme(newTheme);
  }

  setTheme(theme: ThemeMode): void {
    this.currentTheme.set(theme);
    localStorage.setItem(this.THEME_KEY, theme);
    this.applyTheme(theme);
  }

  private applyTheme(theme: ThemeMode): void {
    const htmlElement = document.documentElement;
    if (theme === "dark") {
      htmlElement.classList.add(this.DARK_CLASS);
    } else {
      htmlElement.classList.remove(this.DARK_CLASS);
    }
  }
}

