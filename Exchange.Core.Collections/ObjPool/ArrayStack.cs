using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Exchange.Core.Collections.ObjPool
{
    internal class ArrayStack
    {
        private int count;
        private object[] objects;

        public ArrayStack(int fixedSize)
        {
            this.objects = new object[fixedSize];
            this.count = 0;
        }

        public void Add(object element)
        {
            if (count != objects.Length)
            {
                objects[count] = element;
                count++;
            }
        }

        public object Pop()
        {
            if (count != 0)
            {
                count--;
                object obj = objects[count];
                objects[count] = null;
                return obj;
            }
            return null;
        }
    }

}
