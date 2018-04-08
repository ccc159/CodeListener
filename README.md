**Steps to edit python code from VS Code for Rhino**

## 1. VS Code RhinoPython autocomplete
+ install [VS code](https://code.visualstudio.com/) and [python plugin](https://marketplace.visualstudio.com/items?itemName=ms-python.python)
+ install .Net stubs for IronPython: download [stubs.min](https://github.com/gtalarico/ironpython-stubs/tree/master/release)
+ copy `stubs.min` folder to a safe place (like "C:\Python27\libs\stubs.min")
+ start Rhino, run `_EditPythonScript`, in the python editor, click `tools -> options`, you'll see three module path(depending on your settings you might have more).
+ start VS Code, Click `File -> Preferences -> Settings`, add `stubs.min` and Other folder path mentioned above in the settings json file, like: 
```javascript
"python.autoComplete.extraPaths": [
    "C:\\Python27\\libs\\stubs.min",
    "C:\\Program Files\\Rhinoceros 5 (64-bit)\\Plug-ins\\IronPython\\Lib",
    "C:\\Users\\YOURUSERNAME\\AppData\\Roaming\\McNeel\\Rhinoceros\\5.0\\Plug-ins\\IronPython (814d908a-e25c-493d-97e9-ee3861957f49)\\settings\\lib",
    "C:\\Users\\YOURUSERNAME\\AppData\\Roaming\\McNeel\\Rhinoceros\\5.0\\scripts"
    ]
```
+ now if you create a python file in VS Code, you should see autocomplete for RhinoPython.