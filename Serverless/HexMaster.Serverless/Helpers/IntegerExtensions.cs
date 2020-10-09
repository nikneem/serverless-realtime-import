using System;
using System.Collections.Generic;
using System.Text;

namespace HexMaster.Serverless.Helpers
{
    public static class IntegerExtensions
    {

        public static bool IsSuccessCode(this int input)
        {
            return input >= 200 && input <= 205;
        }

    }
}