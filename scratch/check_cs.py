import os

def check_file(path):
    with open(path, 'r', encoding='utf-8', errors='ignore') as f:
        content = f.read()
    
    in_str = False
    in_char = False
    in_line_comment = False
    in_block_comment = False
    stack = []
    
    i = 0
    while i < len(content):
        c = content[i]
        c2 = content[i:i+2]
        
        if in_line_comment:
            if c == '\n': in_line_comment = False
        elif in_block_comment:
            if c2 == '*/': 
                in_block_comment = False
                i += 1
        elif in_str:
            if c == '\\': i += 1
            elif c == '"': in_str = False
        elif in_char:
            if c == '\\': i += 1
            elif c == "'": in_char = False
        else:
            if c2 == '//': 
                in_line_comment = True
                i += 1
            elif c2 == '/*': 
                in_block_comment = True
                i += 1
            elif c == '"': in_str = True
            elif c == "'": in_char = True
            elif c in '{[(': stack.append((c, i))
            elif c in '}])':
                if not stack:
                    return f'Unmatched closing {c} at pos {i}'
                open_c, _ = stack.pop()
                expected = {'}': '{', ']': '[', ')': '('}[c]
                if open_c != expected:
                    return f'Mismatched {open_c} and {c} at pos {i}'
        i += 1
    if stack:
        return f'Unclosed {stack[-1][0]} at pos {stack[-1][1]}'
    return None

error_count = 0
total_files = 0
for root, dirs, files in os.walk('Assets'):
    for file in files:
        if file.endswith('.cs'):
            total_files += 1
            full = os.path.join(root, file)
            err = check_file(full)
            if err:
                print(f'ERROR in {full}: {err}')
                error_count += 1

print(f'Done checking {total_files} C# files. Found {error_count} errors.')
