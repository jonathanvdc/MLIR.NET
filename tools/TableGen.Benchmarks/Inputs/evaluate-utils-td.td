#ifndef UTILS_TD
#define UTILS_TD

class Deprecated<string reason> {
  string odsDeprecated = reason;
}

class CppDeprecated<string reason> {
  string odsCppDeprecated = reason;
}

class StrFunc<string r> {
  string result = r;
}

def ins;
def outs;

class CArg<string ty, string value = ""> {
  string type = ty;
  string defaultValue = value;
}

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

class snakeCaseToCamelCase<string str>
{
  string ret = !foldl(firstCharToUpper<str>.ret,
    !range(0, !size(str)), acc, idx, _snakeCaseHelper<acc>.ret);
}

#endif // UTILS_TD

def TestDep     : Deprecated<"use something else">;
def TestCppDep  : CppDeprecated<"use CppThing">;
def TestArg     : CArg<"int", "42">;
def TestArgDef  : CArg<"void *">;
def TestCamel   : snakeCaseToCamelCase<"get_op_name">;
