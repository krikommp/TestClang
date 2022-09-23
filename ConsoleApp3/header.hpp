// header.hpp
// /Applications/Xcodebeta.app/Contents/Developer/Platforms/MacOSX.platform/Developer/SDKs/MacOSX10.1 4.sdk/usr/include
#include <iostream>
#include <string>
#include <stdlib.h>
#include <Carbon/Carbon.h>

int TestStdLib(const char* argv)
{
    int ret = atoi(argv);
    return ret + 1;
}

class MyClass
{
public:
    int field;
    virtual void method() const = 0;

    static const int static_field;
    static int static_method();
};