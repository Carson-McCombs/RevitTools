# CarsonsAddins User Guide



***Settings Manager:***

-Enable/Disable which components you want to be registered on and off. Mainly useful for debugging and turning on and off Work In Progress components.

![SettingsManager](https://github.com/Carson-McCombs/RevitTools/assets/130939367/0e5d50e2-d34f-4cd6-8ddf-b287e1d2d2a2)


***Dockable Automatic Pipe End Prep Updater:***

-Dynamically adds all possible fitting flanges and labels each end

-> Setting Up Pipe End Prep Updater

![Opening PEP Window](https://github.com/Carson-McCombs/RevitTools/assets/130939367/590bbbad-0296-4225-9af9-22b3bae1afed)

-> Simple Example

![PEP Updater Example](https://github.com/Carson-McCombs/RevitTools/assets/130939367/d6bf510a-9a95-49ba-8702-2946d2ad39ec)

***Select Pipeline***

-Ending at a change in direction ( i.e. at an bend of any angle, a tee/wye, a cross, etc. )

![Select Pipe Line](https://github.com/Carson-McCombs/RevitTools/assets/130939367/28848fa8-09a6-4e93-9c48-3387d19c7800)

-Ending at a Reducer or Transition piece

![Select Pipe Line to a Reducer](https://github.com/Carson-McCombs/RevitTools/assets/130939367/3f7fe241-68aa-4c85-94a7-342ec9cc0b01)

-Or just at an open connection where a pipe ends

![Select Pipe Line to end of Line](https://github.com/Carson-McCombs/RevitTools/assets/130939367/7a14ef49-39fd-485a-8970-e498a4e20b78)

***Measure the total length of selected pipe***

![Total Pipe Length](https://github.com/Carson-McCombs/RevitTools/assets/130939367/c4853da2-6940-4477-a0d8-b9b185f22c1f)

***Dimension Pipe Line Tool***

-Allows for Piping Elements ( Pipes, Pipe Fittings, and Pipe Accessories ) within a "Pipeline" ( as defined above ) to be dimensions together ( without gaskets / flanges / unions ) in one click. Note: Pipe Accessories and Pipe Fittings both require Reference Lines with the subcategory of "Center line" or "zLines" from the center of each Connector to the center of the element to function correctly.

***Smart Flip***

**Issue:** Not being able to flip a flange without breaking connections and occasionally moving elements.

![Issue with Flipping Flange](https://github.com/Carson-McCombs/RevitTools/assets/130939367/ea94fff4-fd34-4d98-bc9d-c2a573d0d5fd)

**Fix:** Smart Flip which disconnects the elements, flips the flange, then reconnects the elements in one click

![Smart Flip](https://github.com/Carson-McCombs/RevitTools/assets/130939367/e886694a-0e40-4f93-9e51-52ecf9c03f5a)

***Quick Dimension Tool:***

**Issue:** To replace a single dimensions text with a question mark takes you double clicking the dimension, clicking the option to replace the text, then typing in your question mark, and pressing "Apply". -> takes about 5 actions, which is fine unless you need to do multiple.

![Issue with Question mark Dimension](https://github.com/Carson-McCombs/RevitTools/assets/130939367/7e0b6359-6991-45e5-9dab-00b6b714177f)

**Fix:** takes only one click to replace each dimension with a whatever text you would like.

![Question mark Dimension](https://github.com/Carson-McCombs/RevitTools/assets/130939367/cc4e7747-e965-4c1c-b0e0-e21b74061f6d)

***Dockable Element Parameter Assistant:***

-Opening Dockable Pane

![Opening Parameter Manager](https://github.com/Carson-McCombs/RevitTools/assets/130939367/ff5be7bd-3fee-4228-a96d-392de836162b)

-Selecting elements to manager

![Selecting Elements within Parameter Manager](https://github.com/Carson-McCombs/RevitTools/assets/130939367/2286bdb5-58a2-4df5-82a1-c3c1242997ba)

-Can add whatever parameters the elements contains and set them on the fly or group or sort by these parameters.

![Parameter Manager Adding Parameters and Groups](https://github.com/Carson-McCombs/RevitTools/assets/130939367/595dd30f-cbbf-4644-9d78-3e6528fb63f0)

-Easier to find each element parameter instance you would like to set before you set each one

![Parameter Manager Editing a Parameter](https://github.com/Carson-McCombs/RevitTools/assets/130939367/5cf9a0eb-2be0-4ce2-a4a7-7f869672f375)

-Can also set the User's current selection by highlighting specific elements

![Parameter Manager Selecting from Manager](https://github.com/Carson-McCombs/RevitTools/assets/130939367/ce3ab517-99c8-432a-b28f-dde74de6220c)

***Dimension Text Window***

![Uploading image.png…]()


-Functions as the standard Dimension Window except you can apply your changes to multiple Dimensions and DimensionSegments. The only features not currently functioning is swapping between the "Use Actual Value", the "Replace With Text", and "Show Label in View" as well as the Segment Dimension Leader Visibility Dropdown.

