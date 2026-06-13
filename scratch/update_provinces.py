import urllib.request
import json
import re

url = "https://production.cas.so/address-kit/latest/provinces"
req = urllib.request.Request(url, headers={'User-Agent': 'Mozilla/5.0'})

with urllib.request.urlopen(req) as response:
    data = json.loads(response.read().decode('utf-8'))

provinces = data.get('provinces', [])
# Sort by name
provinces.sort(key=lambda x: x['name'])

csharp_lines = []
for p in provinces:
    # new() { Id = "01", Name = "Thành phố Hà Nội" },
    csharp_lines.append(f'            new() {{ Id = "{p["code"]}", Name = "{p["name"]}" }}')

csharp_list_str = ",\n".join(csharp_lines)

filepath = r"C:\Users\juven\Desktop\Fruitables\Data\VietnamAddressData.cs"

with open(filepath, 'r', encoding='utf-8') as f:
    content = f.read()

# Replace everything between `return new List<ProvinceDto>` and `}.OrderBy`
pattern = r'(return new List<ProvinceDto>\s*\{\s*)(.*?)(\s*\}\.OrderBy)'

def replace_func(match):
    return match.group(1) + csharp_list_str + match.group(3)

new_content = re.sub(pattern, replace_func, content, flags=re.DOTALL)

with open(filepath, 'w', encoding='utf-8') as f:
    f.write(new_content)

print("Updated VietnamAddressData.cs successfully.")
