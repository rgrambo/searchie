// Ross Grambo
// INFO 344
// April 29, 2015

$(document).ready(function (e) {

    // Allow for "enter" to submit
    $("#text").keyup(function(event){
        if (event.keyCode == 13) {
            var data = $('#text').val();

            search(data);

            $('#sugbox').hide();
        }
        else
        {
            // Store what was typed
            var temp = $("#text").val();

            search(temp);

            // Make sure something is in the box
            if (temp.length > 0) {
                // Ajax call to Web Service
                $.ajax({
                    type: "GET",
                    url: "WebService1.asmx/startQuery",
                    data: { s: temp },
                    dataType: "json",
                    success: function (msg) {
                        // Clear old suggestions
                        $("#sugbox").empty();
                        $("#sugbox").show();

                        // Make sure the box still has a word in it
                        if (temp.length > 0) {
                            // Make new suggestions
                            $.each(msg, function (index, value) {
                                $('#sugbox').append($('<div class="sug">' + value + '<div>'));
                            });

                            $(".sug").click(function () {
                                $("#text").val($(this).text());
                                $('#sugbox').hide();
                            });
                        }
                    },
                    error: function (xhr, ajaxOptions, thrownError) {
                        alert(xhr.status);
                        alert(thrownError);
                    }
                });
            } else {
                // Empty the suggestions if nothing is in the search box
                $("#sugbox").hide();
            }
        }
    });

    function search(data)
    {
        // Actual Search
        $.ajax({
            type: "GET",
            url: "WebService2.asmx/ReadWithWord",
            data: { 'search': data },
            datatype: "json",
            success: function (msg) {

                $('#main').empty();

                if (msg.length < 1) {
                    var p = document.createElement("p");

                    p.innerHTML = "No Results Found";

                    $('#main').append(p);
                }

                for (var i = 0; i < msg.length; i++) {
                    var a = document.createElement("a");
                    a.href = decodeURIComponent(msg[i]["Url"]);

                    var img = document.createElement("img");
                    img.src = decodeURIComponent(msg[i]["Img"]);
                    img.className = "resultImg";

                    var div = document.createElement("div");
                    div.className = "result";

                    var h2 = document.createElement("h2");
                    h2.innerHTML = msg[i]["Title"];

                    var p = document.createElement("p");
                    p.innerHTML = msg[i]["Url"];
                    p.className = "url";

                    div.appendChild(img);
                    div.appendChild(h2);
                    div.appendChild(p);

                    a.appendChild(div);

                    $('#main').append(a);
                }
            },
            error: function (xhr, ajaxOptions, thrownError) {
                alert(xhr.status);
                alert(thrownError);
            }
        });

        // NBA info
        $.ajax({
            url: 'http://54.186.7.223/search.php',
            type: "GET",
            data: { name: data },
            dataType: "jsonp",
            cache: false,
            success: function (json) {
                if (json) {
                    $('#special').empty();

                    var div = document.createElement("div");
                    div.id = "player";

                    // Found how the nba stores photos
                    var img = document.createElement("img");
                    imageName = json["Name"];
                    imageName = imageName.replace(/\s+/g, '_').toLowerCase();
                    img.src = 'http://i.cdn.turner.com/nba/nba/.element/img/2.0/sect/statscube/players/large/' + imageName + '.png';

                    // Get player name
                    var h1 = document.createElement("h1");
                    h1.innerHTML = json["Name"];

                    // Add table headers
                    var table = document.createElement("table");
                    var tr = document.createElement("tr");
                    var td1 = document.createElement("td");
                    td1.innerHTML = "GP";
                    var td2 = document.createElement("td");
                    td2.innerHTML = "FGP";
                    var td3 = document.createElement("td");
                    td3.innerHTML = "TPP";
                    var td4 = document.createElement("td");
                    td4.innerHTML = "FTP";
                    var td5 = document.createElement("td");
                    td5.innerHTML = "PPG";
                    tr.appendChild(td1);
                    tr.appendChild(td2);
                    tr.appendChild(td3);
                    tr.appendChild(td4);
                    tr.appendChild(td5);
                    table.appendChild(tr);

                    // Add table data
                    var tr = document.createElement("tr");
                    var td1 = document.createElement("td");
                    td1.innerHTML = json["GamesPlayed"];
                    var td2 = document.createElement("td");
                    td2.innerHTML = json["FieldGoalPer"];
                    var td3 = document.createElement("td");
                    td3.innerHTML = json["ThreePointPer"];
                    var td4 = document.createElement("td");
                    td4.innerHTML = json["FreeThrowPer"];
                    var td5 = document.createElement("td");
                    td5.innerHTML = json["PointsPerGame"];
                    tr.appendChild(td1);
                    tr.appendChild(td2);
                    tr.appendChild(td3);
                    tr.appendChild(td4);
                    tr.appendChild(td5);
                    table.appendChild(tr);

                    div.appendChild(img);
                    div.appendChild(h1);
                    div.appendChild(table);

                    // Add the player to the main div
                    $('#special').append(div);
                }
            },
        });
    }

    // This function takes a string and returns json if it is a json String
    // Otherwise it returns false
    function tryParseJSON (jsonString){
        try {
            var o = JSON.parse(jsonString);

            if (o && typeof o === "object" && o !== null) {
                return o;
            }
        }
        catch (e) { }

        return false;
    };

});