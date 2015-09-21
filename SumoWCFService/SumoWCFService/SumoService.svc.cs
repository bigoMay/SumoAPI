using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;

namespace SumoWCFService
{
    [ServiceBehavior(AddressFilterMode = AddressFilterMode.Any)]
    public class SumoService : ISumoService
    {
        public string GetData(int value)
        {
            return string.Format("You entered: {0}", value);
        }

        public CompositeType GetDataUsingDataContract(CompositeType composite)
        {
            if (composite == null)
            {
                throw new ArgumentNullException("composite");
            }
            if (composite.BoolValue)
            {
                composite.StringValue += "Suffix";
            }
            return composite;
        }

        static List<Byte> byteList = new List<Byte>();

        public void setValue(Byte bval)
        {
            byteList.Add(bval);
        }

        public Byte getValue(int index)
        {
            return byteList[index];
        }

        public int lengthOfVal()
        {
            return byteList.Count;
        }
    }
}
