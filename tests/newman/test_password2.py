import re

password = "SysAdmin@2026!"
print("Password:", password)
print("Length:", len(password))
print("Has uppercase:", bool(re.search("[A-Z]", password)))
print("Has lowercase:", bool(re.search("[a-z]", password)))
digit_pattern = "\\d"
print("Has digit:", bool(re.search(digit_pattern, password)))
print("Has special:", bool(re.search("[!@#$%^&*]", password)))
