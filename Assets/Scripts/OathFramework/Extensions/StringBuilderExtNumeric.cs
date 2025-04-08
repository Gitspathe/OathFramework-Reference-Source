/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// File:	StringBuilderExtNumeric.cs
// Date:	9th March 2010
// Author:	Gavin Pugh
// Details:	Extension methods for the 'StringBuilder' standard .NET class, to allow garbage-free concatenation of
//			a selection of simple numeric types.  
//
// Copyright (c) Gavin Pugh 2010 - Released under the zlib license: http://www.opensource.org/licenses/zlib-license.php
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace OathFramework.Extensions
{
    
    public static class StringBuilderExtensions
    {
        // These digits are here in a static array to support hex with simple, easily-understandable code. 
        // Since A-Z don't sit next to 0-9 in the ascii table.
        private static readonly char[] MSDigits = {
            '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F'
        };

        private const uint MSDefaultDecimalPlaces = 5; //< Matches standard .NET formatting dp's
        private const char MSDefaultPadChar = '0';

        //! Convert a given unsigned integer value to a string and concatenate onto the stringbuilder. Any base value allowed.
        public static StringBuilder Concat(this StringBuilder stringBuilder, uint uintVal, uint padAmount, char padChar, uint baseVal)
        {
            Debug.Assert(padAmount >= 0);
            Debug.Assert(baseVal > 0 && baseVal <= 16);

            // Calculate length of integer when written out
            uint length = 0;
            uint lengthCalc = uintVal;

            do {
                lengthCalc /= baseVal;
                length++;
            } while(lengthCalc > 0);

            // Pad out space for writing.
            stringBuilder.Append(padChar, (int)Math.Max(padAmount, length));

            int strpos = stringBuilder.Length;

            // We're writing backwards, one character at a time.
            while(length > 0) {
                strpos--;

                // Lookup from static char array, to cover hex values too
                stringBuilder[strpos] = MSDigits[uintVal % baseVal];
                uintVal /= baseVal;
                length--;
            }

            return stringBuilder;
        }

        //! Convert a given unsigned integer value to a string and concatenate onto the stringbuilder. Assume no padding and base ten.
        public static StringBuilder Concat(this StringBuilder stringBuilder, uint uintVal)
        {
            stringBuilder.Concat(uintVal, 0, MSDefaultPadChar, 10);
            return stringBuilder;
        }

        //! Convert a given unsigned integer value to a string and concatenate onto the stringbuilder. Assume base ten.
        public static StringBuilder Concat(this StringBuilder stringBuilder, uint uintVal, uint padAmount)
        {
            stringBuilder.Concat(uintVal, padAmount, MSDefaultPadChar, 10);
            return stringBuilder;
        }

        //! Convert a given unsigned integer value to a string and concatenate onto the stringbuilder. Assume base ten.
        public static StringBuilder Concat(this StringBuilder stringBuilder, uint uintVal, uint padAmount, char padChar)
        {
            stringBuilder.Concat(uintVal, padAmount, padChar, 10);
            return stringBuilder;
        }

        //! Convert a given signed integer value to a string and concatenate onto the stringbuilder. Any base value allowed.
        public static StringBuilder Concat(this StringBuilder stringBuilder, int intVal, uint padAmount, char padChar, uint baseVal)
        {
            Debug.Assert(padAmount >= 0);
            Debug.Assert(baseVal > 0 && baseVal <= 16);

            // Deal with negative numbers
            if(intVal < 0) {
                stringBuilder.Append('-');
                uint uintVal = uint.MaxValue - (uint)intVal + 1; //< This is to deal with Int32.MinValue
                stringBuilder.Concat(uintVal, padAmount, padChar, baseVal);
            } else {
                stringBuilder.Concat((uint)intVal, padAmount, padChar, baseVal);
            }

            return stringBuilder;
        }

        //! Convert a given signed integer value to a string and concatenate onto the stringbuilder. Assume no padding and base ten.
        public static StringBuilder Concat(this StringBuilder stringBuilder, int intVal)
        {
            stringBuilder.Concat(intVal, 0, MSDefaultPadChar, 10);
            return stringBuilder;
        }

        //! Convert a given signed integer value to a string and concatenate onto the stringbuilder. Assume base ten.
        public static StringBuilder Concat(this StringBuilder stringBuilder, int intVal, uint padAmount)
        {
            stringBuilder.Concat(intVal, padAmount, MSDefaultPadChar, 10);
            return stringBuilder;
        }

        //! Convert a given signed integer value to a string and concatenate onto the stringbuilder. Assume base ten.
        public static StringBuilder Concat(this StringBuilder stringBuilder, int intVal, uint padAmount, char padChar)
        {
            stringBuilder.Concat(intVal, padAmount, padChar, 10);
            return stringBuilder;
        }

        //! Convert a given float value to a string and concatenate onto the stringbuilder
        public static StringBuilder Concat(this StringBuilder stringBuilder, float floatVal, uint decimalPlaces, uint padAmount, char padChar)
        {
            Debug.Assert(padAmount >= 0);

            if(decimalPlaces == 0) {
                // No decimal places, just round up and print it as an int

                // Agh, Math.Floor() just works on doubles/decimals. Don't want to cast! Let's do this the old-fashioned way.
                int intVal;
                if(floatVal >= 0.0f) {
                    // Round up
                    intVal = (int)(floatVal + 0.5f);
                } else {
                    // Round down for negative numbers
                    intVal = (int)(floatVal - 0.5f);
                }

                stringBuilder.Concat(intVal, padAmount, padChar, 10);
            } else {
                int intPart = (int)floatVal;

                // First part is easy, just cast to an integer
                stringBuilder.Concat(intPart, padAmount, padChar, 10);

                // Decimal point
                stringBuilder.Append('.');

                // Work out remainder we need to print after the d.p.
                float remainder = Math.Abs(floatVal - intPart);

                // Multiply up to become an int that we can print
                do {
                    remainder *= 10;
                    decimalPlaces--;
                } while(decimalPlaces > 0);

                // Round up. It's guaranteed to be a positive number, so no extra work required here.
                remainder += 0.5f;

                // All done, print that as an int!
                stringBuilder.Concat((uint)remainder, 0, '0', 10);
            }

            return stringBuilder;
        }

        //! Convert a given float value to a string and concatenate onto the stringbuilder. Assumes five decimal places, and no padding.
        public static StringBuilder Concat(this StringBuilder stringBuilder, float floatVal)
        {
            stringBuilder.Concat(floatVal, MSDefaultDecimalPlaces, 0, MSDefaultPadChar);
            return stringBuilder;
        }

        //! Convert a given float value to a string and concatenate onto the stringbuilder. Assumes no padding.
        public static StringBuilder Concat(this StringBuilder stringBuilder, float floatVal, uint decimalPlaces)
        {
            stringBuilder.Concat(floatVal, decimalPlaces, 0, MSDefaultPadChar);
            return stringBuilder;
        }

        //! Convert a given float value to a string and concatenate onto the stringbuilder.
        public static StringBuilder Concat(this StringBuilder stringBuilder, float floatVal, uint decimalPlaces, uint padAmount)
        {
            stringBuilder.Concat(floatVal, decimalPlaces, padAmount, MSDefaultPadChar);
            return stringBuilder;
        }
    }

}
