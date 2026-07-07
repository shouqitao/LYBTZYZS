import re

password = "SysAdmin@2026!"
print(f"Password: {password}")
print(f"Length: {len(password)}")
print(f"Has uppercase: {bool(re.search(r'[A-Z]', password))}")
print(f"Has lowercase: {bool(re.search(r'[a-z]', password))}")
print(f"Has digit: {bool(re.search(r'\d', password))}")
print(f"Has special: {bool(re.search(r'[!@#$%^&*]', password))}")

# Test sequential numbers
has_sequential = bool(re.search(r'(012|123|234|345|456|567|678|789|890|098|987|876|765|654|543|432|321|210)', password))
print(f"Has sequential numbers: {has_sequential}")

# Test sequential letters
has_sequential_letters = bool(re.search(r'(abc|bcd|cde|def|efg|fgh|ghi|hij|ijk|jkl|klm|lmn|mno|nop|opq|pqr|qrs|rst|stu|tuv|uvw|vwx|wxy|xyz)', password, re.IGNORECASE))
print(f"Has sequential letters: {has_sequential_letters}")

# Test repeating characters
has_repeating = bool(re.search(r'(.)\1{2,}', password))
print(f"Has repeating chars: {has_repeating}")
