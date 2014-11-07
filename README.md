line-diagram
============

C# WPF/XAML line diagram UI component. Draws scalable line diagram based on given dataset. Done as part of "TIEA212 Graphical User Interfaces" coursework. 
```      
Uses two-dimensional STRING array for input. Input is taken as strings, 
but it is required to be in form that can be converted to:
      
x-axis dataformats: double OR date(UTF yyyy-mm-dd)
y-axis dataformats: double
             
EXAMPLE: 
  
  input[0,1] = "1"       // first x-axis value
  input[0,2] = "80"      // first y-axis value
  input[1,1] = "2"       // second x-axis value
  input[1,2] = "80.5"    // second y-axis value
                          
NOTE: x-axis values must be in ascending order and user must handle the ordering.
```
