// Utility classes and definitions for TableGen ODS files.
// Compatible subset of mlir/include/mlir/IR/Utils.td.

#ifndef UTILS_TD
#define UTILS_TD

// Helper for marking deprecated classes or defs in TableGen.
class Deprecated<string reason> {
  string odsDeprecated = reason;
}

// Helper for marking entities in ODS generated C++ as deprecated.
class CppDeprecated<string reason> {
  string odsCppDeprecated = reason;
}

// A workaround for the inability to define functions in TableGen.
// Subclasses can take their own template parameters as "arguments" and use
// them to populate `result`.
class StrFunc<string r> {
  string result = r;
}

// Marker used to identify the argument list.
def ins;

// Marker used to identify the result list.
def outs;

// Represents a typed argument with optional default value for C function
// signatures, e.g. builders or methods.
class CArg<string ty, string value = ""> {
  string type = ty;
  string defaultValue = value;
}

// Makes the first letter of a string uppercase.
class firstCharToUpper<string str>
{
  string ret = !if(!gt(!size(str), 0),
    !toupper(!substr(str, 0, 1)) # !substr(str, 1),
    "");
}

class _snakeCaseHelper<string str> {
  int idx = !find(str, "_");
  string ret = !if(!ge(idx, 0),
    !substr(str, 0, idx) # firstCharToUpper<!substr(str, !add(idx, 1))>.ret,
    str);
}

// Converts a snake_case string to CamelCase.
class snakeCaseToCamelCase<string str>
{
  string ret = !foldl(firstCharToUpper<str>.ret,
    !range(0, !size(str)), acc, idx, _snakeCaseHelper<acc>.ret);
}

#endif // UTILS_TD
