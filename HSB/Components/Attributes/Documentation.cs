using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HSB.Components.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class Documentation : Attribute
{
    private readonly string description;

    public Documentation(string description)
    {
        this.description = description;
    }
    
    public string Description
    {
       get { return description; }
    }
}
