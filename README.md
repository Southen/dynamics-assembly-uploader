# PluginUpdateTool

Microsoft Dynamics CRM Plugin Assembly Update Tool


## Usage

    > git clone https://github.com/Southen/dynamics-assembly-uploader
	> msbuild /property:Configuration=Release PluginUpdateTool.csproj
	> PluginUpdateTool.exe /l
	> PluginUpdateTool.exe /d org.Example
	> PluginUpdateTool.exe /u org.Example.dll

Can be used as a post build step like:
	> PluginUpdateTool.exe /u $(TargetName)



# AutoDeploy

Automagical directory watching Microsoft DYnamics CRM Plugin Assembly Deployer

## Usage
    > git clone https://github.com/Southen/dynamics-assembly-uploader
	> msbuild /property:Configuration=Release AutoDeploy.csproj
	> AutoDeploy.exe



## License

Copyright (c) 2016, Sebastian Southen & Samuel Warnock
All rights reserved.

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <http://www.gnu.org/licenses/>.
