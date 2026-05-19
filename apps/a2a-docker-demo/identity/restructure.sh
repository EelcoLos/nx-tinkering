#!/bin/sh

FILE="$1"

# Use awk to restructure:
# 1. Output all usings first
# 2. Find and output the top-level setup code (const to app.Run())
# 3. Output all the class definitions after app.Run()

awk '
BEGIN {
    usings = ""
    top_level = ""
    classes = ""
    found_const = 0
    in_class = 0
    brace_count = 0
}

/^using / {
    usings = usings $0 "\n"
    next
}

/^const / && !found_const {
    found_const = 1
    top_level = $0 "\n"
    next
}

found_const && !in_class {
    if ($0 ~ /^sealed class|^class |^record / && $0 !~ /\/\//) {
        in_class = 1
        classes = classes $0 "\n"
        for (i = 1; i <= length($0); i++) {
            if (substr($0, i, 1) == "{") brace_count++
            if (substr($0, i, 1) == "}") brace_count--
        }
    } else if ($0 ~ /app\.Run\(\)/) {
        top_level = top_level $0 "\n"
    } else {
        top_level = top_level $0 "\n"
    }
}

in_class {
    classes = classes $0 "\n"
    for (i = 1; i <= length($0); i++) {
        if (substr($0, i, 1) == "{") brace_count++
        if (substr($0, i, 1) == "}") brace_count--
    }
    if (brace_count == 0 && $0 ~ /^}/) {
        in_class = 0
    }
}

!found_const {
    if ($0 !~ /^using / && NF > 0) {
        usings = usings $0 "\n"
    }
}

END {
    printf "%s%s%s", usings, top_level, classes
}
' "$FILE" > "$FILE.new"

mv "$FILE.new" "$FILE"
