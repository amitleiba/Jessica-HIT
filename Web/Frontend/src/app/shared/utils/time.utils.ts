/**
 * Utility functions for time formatting.
 */

/**
 * Formats a duration in milliseconds into a mm:ss string.
 * @param ms Duration in milliseconds
 * @returns Formatted string (e.g. "01:05")
 */
export function formatMs(ms: number): string {
  const totalSeconds = Math.floor(ms / 1000);
  const minutes = Math.floor(totalSeconds / 60);
  const seconds = totalSeconds % 60;
  return `${minutes.toString().padStart(2, '0')}:${seconds.toString().padStart(2, '0')}`;
}
