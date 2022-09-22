#include <iostream>

class MyTestClass
{
public:
    MyTestClass(int InA)
    {
        length = InA;
        ptr = new int(length);
        index = 0;
    }

    ~MyTestClass()
    {
        delete[] ptr;
        ptr = nullptr;
    }

public:

    bool Add(int element)
    {
        if (index >= length) return false;
        ptr[++index] = element;
        return true;
    }

    bool Remove(int element) const
    {
        for (int i = 0; i < length; ++i)
        {
            if (ptr[i] == element)
            {
                int first = i;
                int second = i + 1;
                while (second < length)
                {
                    ptr[first] = ptr[second];
                    ++first;
                    ++second;
                }
                return true;
            }
        }
        return false;
    }
    
public:

    int length;

    int* ptr;

    int index;
};