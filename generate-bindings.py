#!/usr/bin/env python3
"""
Aravis GIR to C# P/Invoke Binding Generator
Parses Aravis GObject Introspection data to generate comprehensive C# bindings
"""

import xml.etree.ElementTree as ET
import os
import sys
from pathlib import Path

# GIR namespace
NS = {'gir': 'http://www.gtk.org/introspection/core/1.0',
      'c': 'http://www.gtk.org/introspection/c/1.0'}

# GLib/GObject functions that should NOT be in the aravis bindings
# (they live in separate libraries: libgobject-2.0, libglib-2.0)
GLIB_PREFIXES = ('g_object_', 'g_error_', 'g_clear_', 'g_free', 'g_signal_',
                 'g_type_', 'g_value_', 'g_param_', 'g_list_', 'g_slist_',
                 'g_main_', 'g_source_', 'g_idle_', 'g_timeout_', 'g_malloc',
                 'g_strdup', 'g_string_', 'g_quark_', 'g_log', 'g_print',
                 'g_variant_', 'g_bytes_')

def is_glib_function(func_name):
    """Check if a function belongs to GLib/GObject and should be excluded"""
    if not func_name:
        return False
    return any(func_name.startswith(prefix) for prefix in GLIB_PREFIXES)

def parse_gir(gir_file):
    """Parse the GIR XML file"""
    tree = ET.parse(gir_file)
    root = tree.getroot()
    return root

def get_csharp_type(gir_type):
    """Convert GIR type to C# type"""
    type_map = {
        'gboolean': 'bool',
        'gint': 'int',
        'guint': 'uint',
        'gint8': 'sbyte',
        'guint8': 'byte',
        'gint16': 'short',
        'guint16': 'ushort',
        'gint32': 'int',
        'guint32': 'uint',
        'gint64': 'long',
        'guint64': 'ulong',
        'gfloat': 'float',
        'gdouble': 'double',
        'gchar': 'byte',
        'gpointer': 'IntPtr',
        'gconstpointer': 'IntPtr',
        'gsize': 'UIntPtr',
        'gssize': 'IntPtr',
        'utf8': 'IntPtr',  # Will be marshalled as string
        'filename': 'IntPtr',
        'none': 'void',
    }
    
    if gir_type in type_map:
        return type_map[gir_type]
    
    # Handle pointer types
    if gir_type and gir_type.endswith('*'):
        return 'IntPtr'
    
    # Default to IntPtr for unknown types
    return 'IntPtr'

def generate_function(func_elem, library_name):
    """Generate C# P/Invoke declaration for a function"""
    func_name = func_elem.get('{http://www.gtk.org/introspection/c/1.0}identifier')
    if not func_name:
        return None
    
    # Get return type
    return_elem = func_elem.find('.//gir:return-value', NS)
    return_type = 'void'
    if return_elem is not None:
        type_elem = return_elem.find('.//gir:type', NS)
        if type_elem is not None:
            return_type = get_csharp_type(type_elem.get('name'))
    
    # Get parameters
    params = []
    has_varargs = False
    params_elem = func_elem.find('.//gir:parameters', NS)
    if params_elem is not None:
        # Check for varargs
        if params_elem.find('.//gir:varargs', NS) is not None:
            has_varargs = True
        
        for param in params_elem.findall('.//gir:parameter', NS):
            param_name = param.get('name', 'param')
            # Fix C# keywords
            csharp_keywords = ['out', 'ref', 'in', 'params', 'event', 'string', 'object', 'delegate',
                               'class', 'interface', 'struct', 'enum', 'namespace', 'using', 'base',
                               'this', 'typeof', 'sizeof', 'new', 'is', 'as', 'lock', 'checked']
            if param_name in csharp_keywords:
                param_name = '@' + param_name
            
            type_elem = param.find('.//gir:type', NS)
            param_type = 'IntPtr'
            if type_elem is not None:
                param_type = get_csharp_type(type_elem.get('name'))
            
            # Handle out parameters
            direction = param.get('direction', 'in')
            if direction == 'out':
                params.append(f"out {param_type} {param_name}")
            elif direction == 'inout':
                params.append(f"ref {param_type} {param_name}")
            else:
                params.append(f"{param_type} {param_name}")
    
    # Skip functions with varargs as they can't be properly P/Invoked
    if has_varargs:
        return None
    
    param_str = ', '.join(params)
    
    code = f"""    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern {return_type} {func_name}({param_str});
"""
    return code

def generate_bindings(gir_file, output_file):
    """Generate complete C# bindings from GIR file"""
    print(f"Parsing {gir_file}...")
    root = parse_gir(gir_file)
    
    namespace_elem = root.find('.//gir:namespace', NS)
    if namespace_elem is None:
        print("ERROR: No namespace found in GIR file")
        return
    
    functions = []
    skipped_glib = []
    seen_names = set()  # Track function names to avoid duplicates
    
    # Find all functions
    for func in namespace_elem.findall('.//gir:function', NS):
        func_name = func.get('{http://www.gtk.org/introspection/c/1.0}identifier')
        if func_name and func_name not in seen_names:
            if is_glib_function(func_name):
                skipped_glib.append(func_name)
                seen_names.add(func_name)
                continue
            code = generate_function(func, None)
            if code:
                functions.append(code)
                seen_names.add(func_name)
    
    # Find all methods in classes
    for klass in namespace_elem.findall('.//gir:class', NS):
        for method in klass.findall('.//gir:method', NS):
            func_name = method.get('{http://www.gtk.org/introspection/c/1.0}identifier')
            if func_name and func_name not in seen_names:
                if is_glib_function(func_name):
                    skipped_glib.append(func_name)
                    seen_names.add(func_name)
                    continue
                code = generate_function(method, None)
                if code:
                    functions.append(code)
                    seen_names.add(func_name)
    
    if skipped_glib:
        print(f"Skipped {len(skipped_glib)} GLib/GObject functions (they belong in GLibNative.cs)")
    
    print(f"Found {len(functions)} unique Aravis functions/methods")
    
    # Generate the C# file
    # Use AravisNative.LibraryName so all resolution goes through the same resolver
    output = f"""using System;
using System.Runtime.InteropServices;

namespace AravisSharp.Generated;

/// <summary>
/// Auto-generated Aravis bindings from GObject Introspection data
/// Generated from: {os.path.basename(gir_file)}
/// Total functions: {len(functions)}
/// </summary>
public static class AravisGenerated
{{
    // Use the same logical name resolved by AravisLibrary.RegisterResolver()
    private const string LibraryName = AravisSharp.Native.AravisNative.LibraryName;

{chr(10).join(functions)}
}}
"""
    
    # Write to file
    os.makedirs(os.path.dirname(output_file), exist_ok=True)
    with open(output_file, 'w') as f:
        f.write(output)
    
    print(f"✓ Generated {output_file}")
    print(f"  {len(functions)} functions exported")

def main():
    gir_file = None
    output_file = "AravisSharp/Generated/AravisGenerated.cs"
    
    # Search for GIR file in multiple locations
    search_paths = [
        # Linux system locations
        "/usr/local/share/gir-1.0/Aravis-0.10.gir",
        "/usr/share/gir-1.0/Aravis-0.8.gir",
        "/usr/local/share/gir-1.0/Aravis-0.8.gir",
        # MSYS2 Windows locations
        "C:/msys64/mingw64/share/gir-1.0/Aravis-0.8.gir",
        "C:/msys64/ucrt64/share/gir-1.0/Aravis-0.8.gir",
        "C:/msys64/clang64/share/gir-1.0/Aravis-0.8.gir",
    ]
    
    for path in search_paths:
        if os.path.exists(path):
            gir_file = path
            print(f"Found GIR file: {gir_file}")
            break
    
    if gir_file is None:
        print("ERROR: No Aravis GIR file found!")
        print("Searched in:")
        for p in search_paths:
            print(f"  {p}")
        return 1
    
    generate_bindings(gir_file, output_file)
    return 0

if __name__ == "__main__":
    sys.exit(main())
