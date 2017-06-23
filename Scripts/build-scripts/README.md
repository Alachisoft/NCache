Guideline for building source code 

1: To build the source code(source, tools and integrations) you have to run "build-all.bat".
2: NC-Regsiter project had unmanage code for registry managment for NCache realated values in registery. This project could not build without visual studio SDK and IDE.
   That ncregister.dll is placed in resource folder for easy of use.
3: If user wants to installed NCache with lasted ncregister project. User have to build nc-register project and then replace updated ncregister.dll in resource folder [by default setup pick-up ncregsiter.dll from resource folder.] 

   