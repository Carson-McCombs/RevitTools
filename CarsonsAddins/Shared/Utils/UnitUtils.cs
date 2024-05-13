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
            public int feetValue;
            public Fraction inchesFraction;

            public FeetAndInchesFraction(double feetAndInches, uint highestDenominator)
            {
                feetValue = (int)feetAndInches;
                double inches = (feetAndInches - feetValue) * 12;
                inchesFraction = new Fraction(inches, highestDenominator);
            }
            public override string ToString()
            {
                List<string> displayString = new List<string>();
                if (feetValue != 0) displayString.Add(feetValue + "\'");
                string fractionString = inchesFraction.ToString();
                if (fractionString != "") displayString.Add(fractionString + '\"');
                return string.Join("-", displayString);
            }
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

                uint numerator = (uint)(Round(decimalValue * highestDenominator));
                if (numerator == highestDenominator)
                {
                    numerator = 0;
                    integerValue += sign;
                }
                double multiple = numerator / (double)highestDenominator;
                uint uint_multiple = (uint)multiple;
                if (multiple % 1 == 0 && uint_multiple > 1) 
                {
                    denominatorValue = highestDenominator / uint_multiple;
                    numeratorValue = numerator / uint_multiple;
                }
                else
                {
                    denominatorValue = highestDenominator;
                    numeratorValue = numerator;
                }
            }
            public override string ToString()
            {
                List<string> displayString = new List<string>();
                if (integerValue != 0) displayString.Add(integerValue.ToString());
                if (numeratorValue != 0) displayString.Add(numeratorValue + "/" + denominatorValue);
                return string.Join(" ", displayString);
            }

        }


        public static double Round(double value)
        {
            int sign = (value >= 0) ? 1 : -1;
            double decimals = value % 1;
            value -= decimals;
            return (decimals >= 0.5) ? value + sign : value;
        }
        
        public static int RoundToInt(double value) => (int)Round(value);

        public static double Floor(double value) => value - (value % 1);
        public static int FloorToInt(double value) => (int)Floor(value); 
        public static double Ceil(double value) => Floor(value) + 1;
        public static int CeilToInt(double value) => (int)Ceil(value);
        public static double RoundIfWithinEpsilon(double value)
        {
            double ceiling = Ceil(value);
            if (ceiling - value <= double.Epsilon) return ceiling;
            double floor = Floor(value);
            if (value - floor <= double.Epsilon) return floor;
            return value;
        }
        public static int RoundIfWithinEpsilonToInt(double value) => (int) RoundIfWithinEpsilon(value);
    }
}
