﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureCompiler.UI
{
    public class VMSizesList
    {
        public static ObservableCollection<string> GetSizes()
        {
            return new ObservableCollection<string> {
                "ExtraSmall",
                "Small",
                "Medium",
                "Large",
                "ExtraLarge",
                "A5",
                "A6",
                "A7",
                "A8",
                "A9",
                "A10",
                "A11",

                "Standard_A1_v2",
                "Standard_A2_v2",
                "Standard_A4_v2",
                "Standard_A8_v2",
                "Standard_A2m_v2",
                "Standard_A4m_v2",
                "Standard_A8m_v2",

                "Standard_D1",
                "Standard_D2",
                "Standard_D3",
                "Standard_D4",
                "Standard_D11",
                "Standard_D12",
                "Standard_D13",
                "Standard_D14",

                "Standard_D1_v2",
                "Standard_D2_v2",
                "Standard_D3_v2",
                "Standard_D4_v2",
                "Standard_D5_v2",
                "Standard_D11_v2",
                "Standard_D12_v2",
                "Standard_D13_v2",
                "Standard_D14_v2",
                "Standard_D15_v2",

                "Standard_D2_v3",
                "Standard_D4_v3",
                "Standard_D8_v3",
                "Standard_D16_v3",
                "Standard_D32_v3",
                "Standard_D64_v3",

                "Standard_E2_v3",
                "Standard_E4_v3",
                "Standard_E8_v3",
                "Standard_E16_v3",
                "Standard_E32_v3",
                "Standard_E64_v3",

                "Standard_G1",
                "Standard_G2",
                "Standard_G3",
                "Standard_G4",
                "Standard_G5",


                "Standard_H8",
                "Standard_H16",
                "Standard_H8m",
                "Standard_H16m",
                "Standard_H16r",
                "Standard_H16mr",
            };
        }
    }
}
