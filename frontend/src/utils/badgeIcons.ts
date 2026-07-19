// Returns corresponding Emoji or icon based on badge name
export const getBadgeIcon = (name: string): string => {
  const iconMap: Record<string, string> = {
    'Green Sprout': '🌱',
    'Eco Guardian': '🌿',
    'Recycling Master': '♻️',
    'Guardian': '🌟',
    'Combo King': '🔥',
    'Eco Legend': '🌏',
    'Recycling Hero': '♻️',
    'Energy Saver': '💡',
    'Tree Planter': '🌳',
    'Eco Warrior': '⚔️',
    default: '🏅',
  };

  return iconMap[name] || iconMap.default;
};