using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CarsonsAddins.Utils
{
    public static class UnitUtils
    {

        public struct FeetAndInchesFraction
        {
            public int feet;
            public Fraction inchesFraction;

            public FeetAndInchesFraction(double feetAndInches, uint highestDenominator)
            {
                feet = (int)feetAndInches;
                double inches = (feetAndInches - feet) * 12;
                inchesFraction = new Fraction(inches, highestDenominator);
            }
            public override string ToString() => feet + "\'-" + inchesFraction.ToString() + '"';
        }


        public struct Fraction
        {
            public int integerValue;
            public uint numeratorValue;
            public uint denominatorValue;  

            public Fraction(double value, uint highestDenominator)
            {
                integerValue = (int)value;
                int sign = (value >= 0) ? 1 : -1;
                
                double decimalValue = (value - integerValue) * sign;

                uint numerator = (uint)(decimalValue * highestDenominator);
                



                double multiple = numerator / (double)highestDenominator;
                if (multiple % 1 == 0 && multiple != 1) 
                {
                    denominatorValue = highestDenominator / (uint)multiple;
                    numeratorValue = numerator / (uint)multiple;
                }
                else
                {
                    denominatorValue = highestDenominator;
                    numeratorValue = numerator;
                }
            }
            public override string ToString() => integerValue + " " + numeratorValue + "/" + denominatorValue;
        }


        

    }
}
