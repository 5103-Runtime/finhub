/**
 * @brief Generates a stable pastel color from a category display name.
 * @param name The category display name.
 * @returns A deterministic HSL color value.
 */
export function getCategoryColor(name: string): string {
  const normalizedName = name.trim().toLowerCase();
  let hash = 0;

  for (const character of normalizedName) {
    hash = (hash * 31 + character.codePointAt(0)!) | 0;
  }

  const hue = Math.abs(hash) % 360;
  return `hsl(${hue} 65% 72%)`;
}
