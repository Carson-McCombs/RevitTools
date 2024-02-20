# RevitTools

Current Placeholder Toolbar UI:
![image](https://github.com/Carson-McCombs/RevitTools/assets/130939367/f59fc2a6-92f0-4d1c-9fb9-af06a11d68f8)

Settings Manager:
-Enable/Disable which components you want to be registered on and off. Mainly useful for debugging and turning on and off Work In Progress components.
![SettingsManager](https://github.com/Carson-McCombs/RevitTools/assets/130939367/0e5d50e2-d34f-4cd6-8ddf-b287e1d2d2a2)

Dockable Automatic Pipe End Prep Updater:
-Dynamically adds all possible fitting flanges and labels each end

-> Setting Up Pipe End Prep Updater

![Opening PEP Window](https://github.com/Carson-McCombs/RevitTools/assets/130939367/590bbbad-0296-4225-9af9-22b3bae1afed)

-> Simple Example

![PEP Updater Example](https://github.com/Carson-McCombs/RevitTools/assets/130939367/d6bf510a-9a95-49ba-8702-2946d2ad39ec)

Select Pipe "Line"

-Ending at a change in direction ( i.e. at an bend of any angle, a tee/wye, a cross, etc. )

![Select Pipe Line](https://github.com/Carson-McCombs/RevitTools/assets/130939367/28848fa8-09a6-4e93-9c48-3387d19c7800)

-Ending at a Reducer or Transition piece

![Select Pipe Line to a Reducer](https://github.com/Carson-McCombs/RevitTools/assets/130939367/3f7fe241-68aa-4c85-94a7-342ec9cc0b01)

-Or just at an open connection where a pipe ends

![Select Pipe Line to end of Line](https://github.com/Carson-McCombs/RevitTools/assets/130939367/7a14ef49-39fd-485a-8970-e498a4e20b78)

Measure the total length of selected pipe

![Total Pipe Length](https://github.com/Carson-McCombs/RevitTools/assets/130939367/c4853da2-6940-4477-a0d8-b9b185f22c1f)

Revit Issue of not being able to flip a flange without breaking connections
![image](https://github.com/Carson-McCombs/RevitTools/assets/130939367/e8d9c1b8-2c78-44ac-8638-0233fbb70b25)

Fix: Smart Flip which disconnects the elements, flips the flange, then reconnects the elements in one click
![image](https://github.com/Carson-McCombs/RevitTools/assets/130939367/3cd7b384-d3c1-47ab-86ae-6a3a93bdb01d)

To replace a single dimensions text with a question mark takes you double clicking the dimension, clicking the option to replace the text, then typing in your question mark, and pressing "Apply". -> takes about 5 actions, which is fine unless you need to do multiple.
![image](https://github.com/Carson-McCombs/RevitTools/assets/130939367/b278a877-5fed-45e9-b2af-9e14cd9f49ac)

Fix: takes only one click to replace each dimension with a whatever text you would like.
![image](https://github.com/Carson-McCombs/RevitTools/assets/130939367/3e858ee9-e16c-4bb4-b725-a4344fc8ddc3)



Dockable Element Parameter Assistance:
-normally much more tedious to find each element parameter instance you would like to set before you set each one
![image](https://github.com/Carson-McCombs/RevitTools/assets/130939367/cbbe09db-9288-48ec-80df-d3784bd380db)

-Can add whatever parameters the elements contains and set them on the fly or group or sort by these parameters.
![image](https://github.com/Carson-McCombs/RevitTools/assets/130939367/b37f2bf9-fb0c-42ae-8926-749ded298979)

-Can also set the User's current selection by highlighting specific elements
![image](https://github.com/Carson-McCombs/RevitTools/assets/130939367/2b9939df-a2e1-4995-94bb-3cd2f7fbe58e)


V2 (12 / 18 / 23): Fixed issue where updaters that were disabled -and therefore unregistered- were attempting to be disabled on shutdown, causing an error.
V3 (12 / 19 / 23): Fixed issue where user was unable to set single segment dimensions. Fixed issue where pipe end prep manager was crashing on enable ( hotfix, not optimal ). Fixed issue where if end preps on a pipe are the same type (bell x bell, spigot x spigot, or none x none), the end prep is alphabetically ordered (unless 'PE').
V4 (12 / 19/ 23 ): Fixed issue where user wouldn't be able to set a dimension in a section 
V5 (12 / 21 / 23 ): Allowed grouped categories for the "Complex Filter" to seperate null and empty strings with and label them appropriately
