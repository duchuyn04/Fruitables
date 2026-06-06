import re

filepath = 'C:/Users/juven/Desktop/Fruitables/Areas/Admin/Views/Order/Detail.cshtml'
with open(filepath, 'r', encoding='utf-8') as f:
    content = f.read()

# Remove darkMode config
content = re.sub(r'darkMode:\s*\"class\",\s*', '', content)

# Remove dark scrollbar css
content = re.sub(r'\.dark \.tailwind-scope \.custom-scrollbar::-webkit-scrollbar-thumb \{[^}]+\}', '', content)

# Fix weird colon broken tailwind classes like hover:bg-slate-100:bg-slate-800
# It should just be hover:bg-slate-100
content = re.sub(r':bg-slate-\d+', '', content)
content = re.sub(r'dark:[^\s\"]+', '', content)

with open(filepath, 'w', encoding='utf-8') as f:
    f.write(content)
