# UnityScripts
A repository for various of my Unity Scripts. Simply for archive/portfolio purposes. Many of my items will have obscured information for my privacy or depending on completion, so you're welcome to interpret the code and use it privately as you wish.


**BTUtility**
- Utility designed for optimization processes (extremely useful for content creation onto platforms such as VRChat, Resonite or ChilloutVR)
 - Creates a window which shows extra details about the selected item (heirarchy or project files). It will access the item's meshed objects and skinned meshes, aswell as any nested objects (if in heirarchy) and add them as rows. Then, there are columns that count triangles (polygons), materials, textures and blendshapes if applicable. Finally, it will show the total size of the object after crunch compression (estimated calculation)
- The utility will also count totals for the selected item, for example, if a VRChat avatar is selected, it will display the total polygons, materials, textures, blendshapes and texture memory size. If an item is clicked it will select it in the heirarchy for quick navigation.
![Unity_DmrweObGOu](https://github.com/user-attachments/assets/2d3e5ec0-4f34-4aba-b5ef-a155d9f35243)
- Image: Tool

**BTPhotoCopy**
- Utility designed to create carbon copies of selected gameobjects with reset references
- Creates a window which prompts user to select object. User may also click item in heirarchy. User is to select what will be carbon copied (textures, materials, fbx, scripts etc)
- Once activated the tool will create a duplicate of the object. However, the duplicate will reference new materials and textures (including nested objects). These new materials and textures replicate the original object's but are organized into their own folders.
![image](https://github.com/user-attachments/assets/9d9a8250-b9dd-4889-9546-e852ab880340)
![image](https://github.com/user-attachments/assets/b64a07be-efbf-4994-b259-729ef766383a)
![image](https://github.com/user-attachments/assets/14940615-b0ce-4297-b0b7-541a96a8cba5)
- Images: Duplicated Object and Children, Organized Save Folders, Applied Duplicated Material on Nested Object

- This tool is extremely useful for developers who wish to create modified copies of objects (eg. with small variations) without having to search through their folders to find the materials or dealing with a huge amount of materials. For example, this reduces the time to develop a "series" of VRChat avatars that have small variations between each other by magnitudes. Or, for map builders, new "buildings" with different textures can be created and have their materials edited directly without impacting the previous buildings.
![image](https://github.com/user-attachments/assets/ffed8cd0-a10c-4bf4-ab10-b91be4361607)
- Image: Tool





## Utility

My content is protected by copyright and Apache 2.0. For private use, knock yourself out. Do not resell my content.

You can install scripts to your Unity project by downloading them directly to your Assets (they are namespaced so they can be placed anywhere). 

I'm not responsible for your use or the functionality of what is published from my utilities


## Usage

After installation my tools appear under the Tools tab in Unity.


## Contributing

Pull requests are welcome. For major changes, please open an issue first

Because I use this reposity for archival purposes it is possible that your change is already implemented
