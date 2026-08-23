import re

with open(r'Assets/Scenes/GamePlay.unity', 'r', encoding='utf-8') as f:
    text = f.read()

# Let's inspect objects in GamePlay.unity
# Find Player GameObject and all related GameObjects
lines = text.split('\n')
for i, line in enumerate(lines[:1000]):
    if 'Player' in line or 'SpriteRenderer' in line or '78188126' in line or 'm_Name:' in line:
        print(f'{i+1}: {line}')
