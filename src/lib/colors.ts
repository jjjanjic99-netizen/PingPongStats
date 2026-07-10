// Categorical + sequential color palette for charts (see dataviz skill).
// Fixed hue order — never cycle/reassign per selection, only per stable index.

export const CATEGORICAL_LIGHT = [
  "#2a78d6", // blue
  "#1baf7a", // aqua
  "#eda100", // yellow
  "#008300", // green
  "#4a3aa7", // violet
  "#e34948", // red
  "#e87ba4", // magenta
  "#eb6834", // orange
] as const;

export const CATEGORICAL_DARK = [
  "#3987e5",
  "#199e70",
  "#c98500",
  "#008300",
  "#9085e9",
  "#e66767",
  "#d55181",
  "#d95926",
] as const;

export const SEQUENTIAL_BLUE_LIGHT = "#2a78d6";
export const SEQUENTIAL_BLUE_DARK = "#3987e5";

export const STATUS = {
  good: { light: "#0ca30c", dark: "#0ca30c" },
  critical: { light: "#d03b3b", dark: "#d03b3b" },
};

/**
 * Returns a stable categorical color for a player based on a fixed index
 * (e.g. sort order by displayName), so the same player keeps the same color
 * across every chart, filter, and re-render. Beyond the 8 defined slots the
 * palette folds into a muted gray rather than generating new hues.
 */
export function categoricalColor(index: number, isDark: boolean): string {
  const palette = isDark ? CATEGORICAL_DARK : CATEGORICAL_LIGHT;
  if (index < palette.length) return palette[index];
  return isDark ? "#6b6b68" : "#898781";
}
