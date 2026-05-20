#!/usr/bin/env python3
"""
Fix C# file structure for .NET 9 compilation with .csproj
Wraps code in a namespace to avoid file-scoped program conflicts
"""
import sys
from pathlib import Path

def fix_cs_file(filepath):
    """Wrap C# code in namespace to fix compilation"""
    with open(filepath, 'r') as f:
        lines = f.readlines()
    
    # Extract directives and usings
    directives = []
    usings = []
    code = []
    
    for line in lines:
        if line.strip().startswith('#:'):
            directives.append(line)
        elif line.strip().startswith('using '):
            usings.append(line)
        else:
            code.append(line)
    
    # Remove leading empty lines from code
    while code and not code[0].strip():
        code.pop(0)
    
    # Rebuild file
    result = []
    
    # Add directives (removed in Dockerfile, but keep for reference)
    for d in directives:
        result.append(d)
    
    # Add usings
    for u in usings:
        result.append(u)
    
    # Add file-scoped namespace
    result.append('\nnamespace A2aDemo;\n\n')
    
    # Add all code (already has classes and app.Run())
    result.extend(code)
    
    with open(filepath, 'w') as f:
        f.writelines(result)

if __name__ == '__main__':
    for arg in sys.argv[1:]:
        fix_cs_file(arg)
        print(f"✓ Fixed {arg}")
