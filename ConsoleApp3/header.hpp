// header.hpp
// /Applications/Xcodebeta.app/Contents/Developer/Platforms/MacOSX.platform/Developer/SDKs/MacOSX10.1 4.sdk/usr/include
#include <iostream>

class MyClass
{
public:
    int field;
    virtual void method() const = 0;

    static const int static_field;
    static int static_method();
};