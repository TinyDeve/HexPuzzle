# HexPuzzle
Simple hex puzzle that you try to match 3 pieces. Completed in two days and i want to improve effect and code efficiency.


Optimizations summary

Code optimizations
*In input update touch delta removed
*Check match and move improved
...

Expensive API calls removed
*Transform.position
*GetComponent<>
...

Garbage Optimization

Collection and array reuse
Array-valued Unity APIs
*Input.touches
Boxing
String Builder

Observed from profiler:
*GC collect frequency is droped.
*GC alloc amount is decreased.


Singelaton game desing used to establish communication between hex and board to pass component information without getComponent.

Sources for garbage optimizations:

*https://docs.unity3d.com/Manual/BestPracticeUnderstandingPerformanceInUnity4-1.html
*https://docs.unity3d.com/Manual/UnderstandingAutomaticMemoryManagement.html
*https://docs.microsoft.com/en-us/dotnet/api/system.text.stringbuilder?view=netframework-4.8

