
---
# <center>Interface</center>
---

### Shader From
The source shader that want to change from.

### Shader To
The destination shader that want to change to.

### Properties List
- The list on the left shows all the properties in the source shader.
- The list in the middle shows all the properties want to convert to, **None** means the property will not be converted.
- The list on the right shows the type of the property.

>:warning:
> If the property type is **None**, the property will not be converted even if the destination is not None.

### "Upgrade Project Folder" Button
Click this button to change the shader and convert properties. This option will scan the whole project folder.

### "Upgrade Selection" Button
Click this button to change the shader and convert properties. If there are materials selected, only the chosen materials will be changed. Otherwise nothing will be changed.

---

# <center>Code API</center>

---

# Enum `PropertyType`
Namescpace: *EditorTools.ShaderPropertiesConverter*

## **Syntax**
```CSharp
public enum PropertyType
```
A enum that contain the type of the property.

## **Fields:**
| Name    | Description | Value |
| :------ | :---------- | :---- |
| None    |
| Texture |
| Float   |
| Vector  |
| Color   |

---

# Class `PropertyData`
Namescpace: *EditorTools.ShaderPropertiesConverter*

## **Syntax**
```CSharp
public class PropertyData
```
This class is used to store the data of shader properties.

## **Property:**
| Name   | Declaration                                   | Description                                        | Type           |
| :----- | :-------------------------------------------- | :------------------------------------------------- | :------------- |
| source | `public string source{ get; set; }`           | represents the name of source shader property      | `string`       |
| dest   | `public string dest { get; set ; } = "None";` | represents the name of destination shader property | `string`       |
| type   | `public PropertyType type{ get; set; }`       | represents the type of shader property             | `PropertyType` |

---

# Class `ShaderPropertiesConverter`
Namescpace: *EditorTools.ShaderPropertiesConverter*

## **Syntax**
```CSharp
public class ShaderPropertiesConverter
```
This class is used to convert shader properties from one shader to another.

## Methods

### `UpgradeProjectFolder(List<PropertyData>, string, string)`
Upgrade all materials in the project folder.

### **Declaration**
```CSharp
UpgradeProjectFolder(List<PropertyData> properties, string oldShader, string newShader)
```

### **Parameters**
| Name       | Description                        | Type                 |
| :--------- | :--------------------------------- | :------------------- |
| properties | List of properties to be converted | `List<PropertyData>` |
| oldShader  | Old shader name                    | `string`             |
| newShader  | New shader name                    | `string`             |

&nbsp;

### `UpgradeSelection(UnityEngine.Object[], List<PropertyData>, string, string)`
Upgrade all materials in the selection

### **Declaration**
```CSharp
UpgradeSelection(UnityEngine.Object[] selection, List<PropertyData> properties, string oldShader, string newShader)
```

### **Parameters**
| Name       | Description                        | Type                   |
| :--------- | :--------------------------------- | :--------------------- |
| selection  | List of objects to be converted    | `UnityEngine.Object[]` |
| properties | List of properties to be converted | `List<PropertyData>`   |
| oldShader  | Old shader name                    | `string`               |
| newShader  | New shader name                    | `string`               |
