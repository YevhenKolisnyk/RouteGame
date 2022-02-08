# RouteGame
Simple geometric game where one needs to build connections and route packets.

UWP app made with C# as a study project.

**Route Game.**
The goal of the game is to build connection network between Nodes (color circles), to let packets (color squares) reach the corresponding node. 
While the node is not connected it could be moved to another position with drag-n-drop. 
Each node could have maximum three links to other nodes.
If there are three links the node is equipped with switch.
When two packets collide thay are destroyed together with the link. 
To build you need points:
 - Node cost {0} points;
 - Link cost {1} points;
 - Delivered package gives you {2} points;
 - Fee for destroying is {3} plus {4} for each package. 
 During the game new base (colored) nodes appear.
Have fun!

![Screenshot](RouteGame\ScreenShot\3.png?raw=true RouteGame UI")
