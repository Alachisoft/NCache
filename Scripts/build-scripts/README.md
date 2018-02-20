# Guideline for Building Source Code #

1. To build the source code (source, tools and integrations) you need to run `build-all.bat`.
2. NC-Register project has unmanage code for registry managment for NCache related values in the registry. This project can not be build without visual studio SDK and IDE. Therefore, the `ncregister.dll` is placed in resource folder for ease of use.
3. If the user wants to install NCache with an updated `NCRegistry` project, (s)he will have to build `NCRegistry` project and then replace the updated `ncregistry.dll` in the 'Resources' folder since by default, the setup picks-up `ncregistry.dll` from the 'Resources' folder.
