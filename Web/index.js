const baseUrl = "http://127.0.0.1:8081"
const board = document.getElementById("mainBoard");

// all cells
const cellList = Array.from(document.getElementsByTagName("li")).filter((li) => {
    return li.childNodes.length != 3 && li.id == "";
});

// get all coordinates available for chess
const getCoordinates = (cellList) => {
    var coordinates = cellList.map((cell) => {
        return { x: cell.getBoundingClientRect().x, y: cell.getBoundingClientRect().y };
    });
    // console.log("coordinates of all cells: ", coordinates);
    // check whether coordinate {x:x, y:y} referred to as coor exists in coordinates referred to as ArrayA
    var exists = (ArrayA, coor) => {
        if (ArrayA.some((item) => item.x == coor.x && item.y == coor.y)) {
            return true
        }
        else {
            return false
        }
    }

    cellList.map((cell) => {
        var newCoordinate = {
            x: cell.getBoundingClientRect().right,
            y: cell.getBoundingClientRect().bottom,
        }
        if (
            !exists(coordinates, newCoordinate)
        ) {
            coordinates.push(newCoordinate);
        }
    });
    // console.log("coordinates of all points: ", coordinates);
    coordinates.push({ x: cellList[64].getBoundingClientRect().left, y: cellList[64].getBoundingClientRect().bottom })
    coordinates.push({ x: cellList[7].getBoundingClientRect().right, y: cellList[7].getBoundingClientRect().top })
    var NewCoordinates = coordinates.map((item) => {
        return { x: item.x - cellList[0].getBoundingClientRect().x, y: item.y - cellList[0].getBoundingClientRect().y }
    }).sort((previous, after) => {
        return previous.y - after.y
    })
    var sorted = []
    for (var i = 0; i < NewCoordinates.length; i += 9) {
        // console.log(NewCoordinates.slice(i, i + 9).sort((previous, after) => {
        //     return previous.x - after.x
        // }))
        sorted.push(NewCoordinates.slice(i, i + 9).sort((previous, after) => {
            return previous.x - after.x
        }))
        // console.log(sorted)
    }
    return sorted;
}

// all coordinates available for chess
const availableLocations = getCoordinates(cellList)

// firstly connect to server
fetch(baseUrl)

var username;
let message_begin = "initial";
var registerStatus = document.getElementById("registerStatus");
var welcome = document.createElement("span")
var join = document.createElement("button");
join.innerText = "Register";
join.addEventListener("click", () => {
    fetch(`${baseUrl}/register`, { method: 'POST' }).then(
        (res) => {
            res.text().then((res) => {
                console.log(res);
                username = res;
                welcome.innerText = `Welcome, you have been assigned with name ${username}`;
                registerStatus.appendChild(welcome);
                registerStatus.removeChild(join)
                var pair = document.createElement("button")
                pair.innerText = "Join Game"
                pair.addEventListener("click", () => {
                    registerStatus.appendChild(document.createElement("br"));
                    var msg_wait = document.createElement("span")
                    msg_wait.innerText = "waiting for another player..."
                    registerStatus.appendChild(msg_wait);
                    registerStatus.removeChild(pair);
                    pairme(username, msg_wait)
                })
                registerStatus.appendChild(document.createElement("br"));
                registerStatus.appendChild(pair);
            })
        })
}
)

registerStatus.appendChild(join);

const pairme = (username, msg_wait) => {
    fetch(`${baseUrl}/pair?player=${username}`, { method: 'POST' })
        .then((resp) => {
            resp.text().then((resp => {
                const resObj = JSON.parse(resp)
                if (!resObj.msg.includes("good luck")) {
                    setTimeout(() => {
                        pairme(username, msg_wait)
                    }, 5000)
                }
                else {
                    // successfully paired
                    // 1. notify the begin of game
                    message_begin = resObj.msg
                    var msg_begin = document.createElement("span")
                    msg_begin.innerText = message_begin
                    registerStatus.removeChild(msg_wait)
                    registerStatus.appendChild(msg_begin);

                    var logout = document.createElement("button")
                    logout.innerText = "Leave Game";
                    logout.addEventListener("click", () => {
                        fetch(`${baseUrl}/quit?player=${resObj.username}&gameID=${resObj.gameID}`, { method: 'POST' });
                        alert("You have left game")
                        location.reload()
                    })
                    registerStatus.appendChild(document.createElement("br"))
                    registerStatus.appendChild(document.createElement("br"))
                    registerStatus.appendChild(logout)

                    // 2. initiate chesses
                    if (resObj.label == "2") {
                        document.getElementById("title").style.transform = "rotate(180deg)";
                        document.getElementById("outBorder").style.transform = "rotate(180deg)";
                        document.getElementById("chuhe").style.transform = "rotate(180deg)";
                        document.getElementById("hanjie").style.transform = "rotate(180deg)";
                    }
                    var chessLayout = fetch(`${baseUrl}/initialstatus?player=${resObj.username}&gameID=${resObj.gameID}`, { method: 'POST' })
                        .then(
                            (res) => {
                                return (res.text().then((res) => {
                                    return Array.from(JSON.parse("[" + res + "]"))
                                }))
                            })

                    chessLayout.then((res) => {
                        // res: initial chess layout

                        // render a chess, 
                        // args -- 
                        // faction: black or red; 
                        // division: '炮', '兵', etc, 
                        // location: e.g. availableLocations, 
                        // x&y: index of location, e.g. x=5, y=1 means the point on row 6, collum 2
                        const renderChess = (res, faction, division, location, x, y) => {
                            if (faction == "null" || division == "null") {
                                return null;
                            } else {
                                res[x * 9 + y].faction = faction;
                                res[x * 9 + y].division = division;
                                var chess = document.createElement('div');
                                chess.className = `chessPieces ${faction}`;
                                var innerDiv = document.createElement('div');
                                var spanElement = document.createElement('span');
                                spanElement.className = "divisionName";
                                spanElement.textContent = division;
                                innerDiv.appendChild(spanElement);
                                chess.id = JSON.stringify({ x: x, y: y })
                                chess.appendChild(innerDiv);
                                chess.style.position = "absolute"
                                chess.style.left = location[x][y].x + 47.5;
                                chess.style.top = location[x][y].y + 65;
                                if (resObj.label == "1") {
                                    // red side
                                    if (chess.className.includes("red")) {
                                        chess.addEventListener("click", () => {
                                            try {
                                                var others = document.getElementsByClassName("chessPieces expected");
                                                if (others.length != 0) {
                                                    Array.from(others).map((item) => board.removeChild(item))
                                                }
                                                curr_loc = JSON.parse(chess.id)
                                                var available_loc;
                                                available_loc = getAvailableLoc(division, curr_loc)
                                                available_loc.map((item) => {
                                                    var availableLocation = document.createElement('div');
                                                    availableLocation.className = "chessPieces expected";
                                                    availableLocation.style.position = "absolute"
                                                    availableLocation.style.left = location[item.x][item.y].x + 47.5;
                                                    availableLocation.style.top = location[item.x][item.y].y + 65;
                                                    availableLocation.addEventListener("click", () => {
                                                        moveChess(faction, division, location, x, y, item.x, item.y, board)
                                                    })
                                                    board.appendChild(availableLocation);
                                                    chess.addEventListener("dblclick", () => {
                                                        try {
                                                            board.removeChild(availableLocation)
                                                        }
                                                        catch (e) {
                                                            console.log(e)
                                                        }
                                                    })
                                                })
                                            } catch (e) {
                                                console.log(e)
                                            }
                                        })
                                    }
                                }
                                else if (resObj.label == "2") {
                                    // red side
                                    if (chess.className.includes("black")) {
                                        chess.addEventListener("click", () => {
                                            try {
                                                var others = document.getElementsByClassName("chessPieces expected");
                                                if (others.length != 0) {
                                                    Array.from(others).map((item) => board.removeChild(item))
                                                }
                                                curr_loc = JSON.parse(chess.id)
                                                var available_loc;
                                                available_loc = getAvailableLoc(division, curr_loc)
                                                available_loc.map((item) => {
                                                    var availableLocation = document.createElement('div');
                                                    availableLocation.className = "chessPieces expected";
                                                    availableLocation.style.position = "absolute"
                                                    availableLocation.style.left = location[item.x][item.y].x + 47.5;
                                                    availableLocation.style.top = location[item.x][item.y].y + 65;
                                                    availableLocation.addEventListener("click", () => {
                                                        moveChess(faction, division, location, x, y, item.x, item.y, board)
                                                    })
                                                    board.appendChild(availableLocation);
                                                    chess.addEventListener("dblclick", () => {
                                                        try {
                                                            board.removeChild(availableLocation)
                                                        }
                                                        catch (e) {
                                                            console.log(e)
                                                        }
                                                    })
                                                })
                                            } catch (e) {
                                                console.log(e)
                                            }
                                        })
                                    }
                                }
                                board.appendChild(chess);
                            }
                        }

                        const deleteChess = (res, x, y) => {
                            var chess_to_delete = document.getElementById(JSON.stringify({ x: x, y: y }));
                            board.removeChild(chess_to_delete);
                            res[x * 9 + y].faction = res[x * 9 + y].division = "null"
                        }

                        const moveChess = (faction, division, location, curr_x, curr_y, tar_x, tar_y, board) => {
                            deleteChess(res, curr_x, curr_y);
                            renderChess(res, faction, division, location, tar_x, tar_y);
                            var availableLocation = Array.from(document.getElementsByClassName("expected"));
                            availableLocation.map((item) => {
                                board.removeChild(item);
                            })
                            fetch(`${baseUrl}/mymove?player=${resObj.username}&gameID=${resObj.gameID}&orow=${curr_x}&ocol=${curr_y}&nrow=${tar_x}&ncol=${tar_y}`, { method: 'POST' })
                        }
                        res.map((item) => {
                            renderChess(res, item.faction, item.division, availableLocations, item.row, item.col)
                        })
                        // get available location based on basic moving rules
                        // args -- 
                        // division: '炮', '兵', etc, 
                        // curr_loc: {x:x, y:y}: index of location, e.g. 5, 1 means the point on row 2, collum 6
                        const getAvailableLoc = (division, curr_loc) => {
                            var targets = [];
                            left_slot = []// number of all other points on the left
                            for (var i = 1; i <= curr_loc.y; i++) {
                                left_slot.push(i)
                            }
                            var right_slot = [] // number of all other points on the right
                            for (var i = 1; i <= 8 - curr_loc.y; i++) {
                                right_slot.push(i)
                            }
                            var above_slot = []// number of all other points above
                            for (var i = 1; i <= curr_loc.x; i++) {
                                above_slot.push(i)
                            }
                            var below_slot = [] // number of all other points below
                            for (var i = 1; i <= 9 - curr_loc.x; i++) {
                                below_slot.push(i)
                            }
                            var horse_slot = [
                                { x: curr_loc.x - 1, y: curr_loc.y - 2 },
                                { x: curr_loc.x - 1, y: curr_loc.y + 2 },
                                { x: curr_loc.x - 2, y: curr_loc.y - 1 },
                                { x: curr_loc.x - 2, y: curr_loc.y + 1 },
                                { x: curr_loc.x + 1, y: curr_loc.y - 2 },
                                { x: curr_loc.x + 1, y: curr_loc.y + 2 },
                                { x: curr_loc.x + 2, y: curr_loc.y - 1 },
                                { x: curr_loc.x + 2, y: curr_loc.y + 1 },
                            ].filter((item) => item.x >= 0 && item.x <= 9 && item.y >= 0 && item.y <= 8) // number of all other points for horse

                            var elephant_slot = [
                                { x: curr_loc.x - 2, y: curr_loc.y - 2 },
                                { x: curr_loc.x - 2, y: curr_loc.y + 2 },
                                { x: curr_loc.x + 2, y: curr_loc.y - 2 },
                                { x: curr_loc.x + 2, y: curr_loc.y + 2 },
                            ].filter((item) => item.x >= 0 && item.x <= 9 && item.y >= 0 && item.y <= 8) // number of all other points for elephant

                            switch (division) {
                                case '砲':
                                case '炮':
                                    if (left_slot.length != 0) {
                                        left_slot.map((item) => {
                                            targets.push({ x: curr_loc.x, y: curr_loc.y - item })
                                        })
                                    }
                                    if (right_slot.length != 0) {
                                        right_slot.map((item) => {
                                            targets.push({ x: curr_loc.x, y: curr_loc.y + item })
                                        })
                                    }
                                    if (above_slot.length != 0) {
                                        above_slot.map((item) => {
                                            targets.push({ x: curr_loc.x - item, y: curr_loc.y })
                                        })
                                    }
                                    if (below_slot.length != 0) {
                                        below_slot.map((item) => {
                                            targets.push({ x: curr_loc.x + item, y: curr_loc.y })
                                        })
                                    }
                                    break
                                case '馬':
                                    if (horse_slot.length != 0) {
                                        horse_slot.map((item) => {
                                            targets.push(item)
                                        })
                                    }
                                    break;
                                case '相':
                                case '象':
                                    if (elephant_slot.length != 0) {
                                        elephant_slot.map((item) => {
                                            targets.push(item)
                                        })
                                    }
                                    break;
                            }

                            var targets_filtered = []

                            targets.map((item) => {
                                const occupation = checkWhetherOccupied(res, item.x, item.y)
                                if (occupation == "empty") {
                                    targets_filtered.push({ x: item.x, y: item.y })
                                }
                            })

                            return targets_filtered;
                        }
                        // check whether a point is occupied
                        const checkWhetherOccupied = (layout, row, col) => {
                            const test = layout[row * 9 + col]
                            if (test.faction == "null" || layout[row * 9 + col].division == "null") {
                                return "empty"
                            }
                            else {
                                return { fac: test.faction, div: test.division }
                            }
                        }
                    }
                    ).catch(() => { console.log("invalid request") })


                }
            }))
        })
}

window.addEventListener("beforeunload", (e) => {
    e.preventDefault();
    alert("If you refresh this page, you would be logged out and game data would be lost")
    location.reload()
    e.returnValue = ""
})