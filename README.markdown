JPG Corruptor
====================
Copyright © 2012 Charlie Kindel.
Licensed under the BSD License.

A digital art expiriment.
---------------------

JPG Corruptor was the inspiration of my brother-in-law who is an art student. 

The idea is to inject the text of a book into a JPG file corrupting. It turns out that most
JPG files are quite reslient to such corruption and the resulting images can be quite interesting. 

## Instructions
* Run this little Windows app.
* Choose a text file (e.g. the first chapter of The Great Gatsby)
* Choose a JPG (e.g. an image from the movie The Great Gatsby)
* Click on the Go (or Go Full Screen) button
* Watch it unfold
* When in full screen mode, ESC will stop it

## Notes
* Some JPG files get massively corrupted really quickly. It appears that those saved by Photoshop are more resilient than others.
* The larger the JPG image the slower the app is, but the more interesting the results.
* Each word of the text file overwrites JPG data at a random spot. 
* I tried inserting the text data but that caused the files to be quickly unreadable.

## Future work
* I currently avoid overwitting JPG data in the first 256 bytes of the file. I intentionally didn't read any JPG specs but I assumed there's some form of header. I plan on experimenting with this.
* The text is randomly spread around the file.  I plan on implementing a mode where it over writes JPG data sequentially (or at least biasing it from top to bottom over time).