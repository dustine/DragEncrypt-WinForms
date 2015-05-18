#DragEncrypt
A simple programming toy that encrypts and/or decrypts any files that are dragged to it with an user-chosen password key.

##How to Use
Drag any binary file into the executable file. Simply opening the executable will open a file dialogue prompt as an alternative method to pick the targeted file.

A window will appear where the user can insert the desired password, with a holdable button to peek into it. 

Decryption is done with the same method as encrypting, just requiring the encrypted file to be the chosen target.

##Safety concerts
The program takes no extra security precautions during its run, beyond trying to delete the unecrypted password 'quickly', so the process is rather vulnerable to attacks before and during encryption, likewise for decryption. However the created encrypted file *should* be mostly safe from brute-force attacks or re-engineering, or at least [as safe](https://howsecureismypassword.net) as the chosen password. 

##Contributing
<div xmlns:cc="http://creativecommons.org/ns#" xmlns:dct="http://purl.org/dc/terms/" about="http://www.wpzoom.com/wpzoom/new-freebie-wpzoom-developer-icon-set-154-free-icons/">This software contains icons from the <span property="dct:title">WPZOOM Developer Icon Set</span> (<a rel="cc:attributionURL" property="cc:attributionName" href="http://www.wpzoom.com">WPZOOM</a>) / <a rel="license" href="http://creativecommons.org/licenses/by-sa/3.0/">CC BY-SA 3.0</a>.</div>
