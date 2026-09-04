mergeInto(LibraryManager.library, {

    Alert : function(message) {
        console.log("Alert");
        alert(UTF8ToString(message));
    },

    Prompt : function(message) {
        console.log("Prompt");
        
        let text = prompt(UTF8ToString(message), "");
        if (text != null) {
            var bufferSize = lengthBytesUTF8(text) + 1;
            var buffer = _malloc(bufferSize);
            stringToUTF8(text, buffer, bufferSize);
            return buffer;
        }
        
        return "";
    },
    
    DownloadToFile: function (content, filename) {
        console.log("DownloadToFile");
    
        const contentStr = UTF8ToString(content);
        const filenameStr = UTF8ToString(filename);

        const blob = new Blob([contentStr], { type: "application/json" });
        const url = URL.createObjectURL(blob, { oneTimeOnly: true });

        const element = document.createElement("a");
        element.href = url;
        element.download = filenameStr;
        element.style.display = "none";
        document.body.appendChild(element);

        element.click();

        document.body.removeChild(element);
    },
    
    DownloadFile : function(array, size, fileNamePtr)
    {
        var fileName = UTF8ToString(fileNamePtr);
 
        var bytes = new Uint8Array(size);
        for (var i = 0; i < size; i++)
        {
           bytes[i] = HEAPU8[array + i];
        }
 
        var blob = new Blob([bytes]);
        var link = document.createElement('a');
        link.href = window.URL.createObjectURL(blob);
        link.download = fileName;
 
        var event = document.createEvent("MouseEvents");
        event.initMouseEvent("click");
        link.dispatchEvent(event);
        window.URL.revokeObjectURL(link.href);
    },

    UploadFile: function () {
        console.log("UploadFile");
        
        var fileuploader = document.getElementById("fileuploader");
        if (!fileuploader) {
            fileuploader = document.createElement("input");
            fileuploader.setAttribute("style","display:none;");
            fileuploader.setAttribute("type", "file");
            fileuploader.setAttribute("class", "native-file-input");
            fileuploader.setAttribute("id", "fileuploader");
            fileuploader.setAttribute("multiple", "false");
            document.getElementsByTagName("body")[0].appendChild(fileuploader);
            
            fileuploader.onchange = function(e) {
                var files = e.target.files;
                for (var i = 0, f; f = files[i]; i++) {
                    SendMessage('WebGLMessageHandler', 'HandleFile', URL.createObjectURL(f));
                }
            }
        }
        fileuploader.click();
    },
    
    SaveToLocalStorage : function(key, data) {
        console.log("SaveToLocalStorage");
        try {
            localStorage.setItem(UTF8ToString(key), UTF8ToString(data));
            return true;
        } catch (e) {
            return false;
        }
    },
    
    LoadFromLocalStorage : function(key) {
        console.log("LoadFromLocalStorage");
        var returnStr = localStorage.getItem(UTF8ToString(key));
        if (returnStr) {
            var bufferSize = lengthBytesUTF8(returnStr) + 1;
            var buffer = _malloc(bufferSize);
            stringToUTF8(returnStr, buffer, bufferSize);
            return buffer;
        }
        return "";
    },
    
    RemoveFromLocalStorage : function(key) {
        console.log("RemoveFromLocalStorage");
        localStorage.removeItem(UTF8ToString(key));
    },
    
    HasKeyInLocalStorage : function(key) {
        console.log("HasKeyInLocalStorage");
        if (localStorage.getItem(UTF8ToString(key))) {
          return 1;
        }
        else {
          return 0;
        }
    },
    
    GetAllLocalStorageKeys : function(prefix, separation) {
        console.log("GetAllLocalStorageKeys");
    
        const prefixStr = UTF8ToString(prefix);
        const separationStr = UTF8ToString(separation);
        
        var keys = Object.keys(localStorage);
        console.log(keys);
        
        var result = "";
        for (const key of keys) {
            if (key.startsWith(prefixStr)) {
                console.log(key);
                result += key + separationStr;
            }
        }
        result = result.slice(0, -1);
        
        var bufferSize = lengthBytesUTF8(result) + 1;
        var buffer = _malloc(bufferSize);
        stringToUTF8(result, buffer, bufferSize);
        return buffer;
    },
});