# Azure package compiler

## Solution structure

Program have two versions of application. With UI and console application. You can find them inside solution AzureCompiler.

- AzureCompiler.Console - console application
- AzureCompiler.UI - UI application
- AzureCompiler.Core - main algorithm for compilation package.
- DummySite - site using for pre installation acumatica.
- PX.Dummy - required for DummySite.
- PX.Azure - configuration for Acumatica Role in Azure

## Before run application

You need to install Azure SDK.
You can do it Visual Studio installer for version (2017, 2019). Just select it as workload in installation.
![Selected AzureSDK](/docs/images/install.png "AzureSDK")

## AzureCompiler.UI

![Opened UI application](/docs/images/ui1.png "EmptyUI")

- Path to Azure SDK - required. Path to azure packaging tool.
- Acumatica Version - required. Default value "2020R1/2019R2". Using for prefere which version of NET Framework required to use
- VM Size - required, if selected radio button "Standard Acumatica config". Defaule value - Medium. Define virtual machine size for which will be compiled azure package. If you don't  find required size, you can write it in the field. Also it will be use standard acumatica configuration for azure packages.
- Custom config - required, if selected radio button "Custom config".Define full path to file *.csdef. In this case application didn't use VM Size.
- Path to source folder - required. Path to unpacked ErpPackage.zip. It can be downloaded from [builds](http://builds.acumatica.com/)
- Path to output folder - required. Path to directory where is to save azure package. If you use standard config - output files name will be like this "Standard_{Vm SIze}.cspkg",
if you use custom config - output file name will be "AzurePackage.cspkg"

Example filled fields
![Opened UI application - 2](/docs/images/ui2.png "FilledUI")

After each run application store selected settings in user directory.