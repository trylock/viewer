using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Viewer.Data
{
    public interface IAttributeVisitor
    {
        void Visit(IntAttribute attr);
        void Visit(DoubleAttribute attr);
        void Visit(StringAttribute attr);
        void Visit(DateTimeAttribute attr);
        void Visit(ImageAttribute attr);
    }

    public interface IAttributeVisitor<out T>
    {
        T Visit(IntAttribute attr);
        T Visit(DoubleAttribute attr);
        T Visit(StringAttribute attr);
        T Visit(DateTimeAttribute attr);
        T Visit(ImageAttribute attr);
    }
}
