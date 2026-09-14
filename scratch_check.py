
import urllib.request
url = 'https://github.com/mrrichar11/puntoVentaMR/releases'
req = urllib.request.Request(url, headers={'User-Agent': 'Mozilla/5.0'})
resp = urllib.request.urlopen(req)
html = resp.read().decode('utf-8', errors='ignore')
print('Tags in text:', 'v1.0.1' in html, 'v1.0.0' in html)
print('Releases in text:', 'There are no releases' in html or 'No releases published' in html)
