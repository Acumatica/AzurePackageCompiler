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

- Path to Azure SDK - **Required**. Path to azure packaging tool cspack.exe.
- Acumatica Version - **Required**. Default value "2020R1/2019R2". Use for prefere which version of NET Framework required install before start Acumatica ERP
- VM Size - **Required, if selected radio button "Standard Acumatica config"**. Defaule value - Medium. Define virtual machine size for which will be compiled azure package. If you don't  find required size, you can write it in the field. Application will be use standard Acumatica configuration for azure packages.
- Custom config - **Required, if selected radio button "Custom config"**. Define full path to file *.csdef. In this case application didn't use VM Size.
- Path to source folder - **Required**. Path to unpacked ErpPackage.zip. It can be downloaded from [builds](http://builds.acumatica.com/)
- Path to output folder - **Required**. Path to directory where is to save azure package. If you use standard config - output files name will be like this "Standard_{Vm SIze}.cspkg",
if you use custom config - output file name will be "AzurePackage.cspkg"

Example filled fields
![Opened UI application - 2](/docs/images/ui2.png "FilledUI")

After each run application store selected settings in user directory.

### AzureCompiler.Console

Tools for compilation azure package in a build system.

- -c, --cspack       **Required**. Path to azure packaging tool cspack.exe
- -s, --sourceDir    **Required**. Path to source folder, unpacked ErpPackage.zip
- -o, --outDir       **Required**. Path to output folder
- -f, --framework    **Required**. Framework version. Avaiable values: NDP48 - for version 2019R2/2020R1, NDP482 - 2019R1
- -v, --vmSize       Vitrual machine size. Can be skipped if you use custom config.
- -g, --config       Path to your custom config file (*.csdef). Can be skipped if you use standard config.
- -q, --quiet        (Default: false) Closing window when program is finished.
