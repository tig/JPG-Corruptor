JPG Corruptor
====================
Copyright © 2012 Charlie Kindel ([@ckindel] (http://twitter.com/ckindel) on Twitter)
Licensed under the BSD License.

Version 1.1

A digital art expiriment.
---------------------

JPG Corruptor has been inspired and commissioned by my brother-in-law who is an art student. He
is using it for a project in an art class.

The idea is to create a "movie" of a JPG file being corrupted through the overwriting of JPG binary data with words from the text of a book. It turns out that most JPG files are quite reslient to such corruption and the resulting images, when rendered, can be quite interesting. This application takes a text file and a JPG file and displays each iteration of the JPG file as it is corrupted by the words in the text file.

The JPG file is "corrupted" by randomly overwriting data in the file with the words in the text file. 

![Example output](http://farm8.staticflickr.com/7149/6834346187_446618ec76.jpg "Example JPG Corruptor Output; First two chapters of The Great Gatsby")  
Example JPG Corruptor Output; What [this image](http://www.filmcritic.com/assets_c/2010/02/The-Great-Gatsby-thumb-560xauto-25948.gif) looks like after being corrupted by the first two chapters of The Great Gatsby

Watch a video of JPG Corruptor in action: http://youtu.be/iTtsAL7sSyc

## Download
[JPG Corruptor app for Windows] (https://github.com/downloads/tig/JPG-Corruptor/JPGCorrupt.zip)

## Instructions
* Run this little Windows app.
* Choose a text file (e.g. the first chapter of The Great Gatsby)
* Choose a JPG (e.g. an image from the movie The Great Gatsby)
* Click on the Go (or Go Full Screen) button
* Watch it unfold
* When in full screen mode, ESC will stop it
* The "Save Current" button will let you save the very latest frame.
* Loop mode will cause the corruption process to repeat over and over.

## Notes
* Some JPG files get massively corrupted really quickly. *It appears that those saved by Photoshop are more resilient than others*.
* Some JPG renderers display the corrupt JPGs differently. FireFox, IE, and Chrome all have subtle differences. Windows picture preview is *very* different. 
* Flickr refuses to upload some of these images.
* Photoshop tends to not want to open these files
* Paint (Windows) opens them fine, but fails to save them.
* **Paint.NET opens them fine and saves them fine**. After it saves them they appear to be openable by anything (e.g. Flickr upload works.)
* The larger the JPG image the slower the app is, but the more interesting the results.
* Each word of the text file overwrites JPG data at a random spot. 
* I tried inserting the text data (instead of overwriting) but that caused the files to be quickly unreadable.

## Version History
* 1.0 - First release for Tom
* 1.1 - Addressed feedback from Tom: Background is now black, Loop mode, removed text display.

## Future work
* I currently avoid overwitting JPG data in the first 256 bytes of the file. I intentionally didn't read any JPG specs but I assumed there's some form of header. I tried 64 bytes and got unreadable files quickly.
* The text is randomly spread around the file.  I tried implementing a mode where it over writes JPG data sequentially but it regularly completely corrupted the file.
* Originally I implemented this such that it saved every JPG along the way. It now works completely in memory, but I added the ability to save an image at any point.