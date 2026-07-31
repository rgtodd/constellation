Generate a web application that creates images of constellation boundaries.

The UI should include:
* Latitude and Longitude of viewer.
* Date and time of observation in local time.
* The constellation to render (drop-down).

The boundary of the constellation should oriented for a viewer at the specific location, date and time. Each image should be 600 by 600 pixels. The constellation boundary should be sized to 550 by 550 pixels and centered within the image. The region of the constellation should be white. The region outside the constellation should be grey. 

Files containing constellation boundaries is the folder: constellation/wwwroot/boundaries

The standard constellation abbreviation is used as the file name. 

The format of each file is:

HH MM SS.SSSS| DD.DDDDDDD|XXX

Where:
* HH MM SS.SSSS defines the right ascension hour, minute and second with J2000 coordinates
* DD.DDDDDDD defines the declination with J2000 coordinates
* XXX is the abbreviation of the constellation name
* | is the separator of the fields

Example: 22 57 51.6729| 35.1682358|AND


