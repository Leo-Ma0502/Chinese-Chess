var board = document.getElementById("mainBoard");
board.addEventListener("click", (e) => {
    console.log(e.clientX - cellList[0].getBoundingClientRect().x, e.clientY - cellList[0].getBoundingClientRect().y);
});

// all cells
var cellList = Array.from(document.getElementsByTagName("li")).filter((li) => {
    return li.childNodes.length != 3 && li.id == "";
});

// get all coordinates available for chess
var getCoordinates = (cellList) => {
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

// initiate chesses
var initiate = (availableLocations) => {
    renderChess('red', '砲', availableLocations, 7, 1)
    renderChess('red', '砲', availableLocations, 7, 7)
    renderChess('red', '兵', availableLocations, 6, 0)
    renderChess('red', '兵', availableLocations, 6, 2)
    renderChess('red', '兵', availableLocations, 6, 4)
    renderChess('red', '兵', availableLocations, 6, 6)
    renderChess('red', '兵', availableLocations, 6, 8)
    renderChess('red', '車', availableLocations, 9, 0)
    renderChess('red', '車', availableLocations, 9, 8)
    renderChess('red', '馬', availableLocations, 9, 1)
    renderChess('red', '馬', availableLocations, 9, 7)
    renderChess('red', '相', availableLocations, 9, 2)
    renderChess('red', '相', availableLocations, 9, 6)
    renderChess('red', '仕', availableLocations, 9, 3)
    renderChess('red', '仕', availableLocations, 9, 5)
    renderChess('red', '帥', availableLocations, 9, 4)

    renderChess('black', '炮', availableLocations, 2, 1)
    renderChess('black', '炮', availableLocations, 2, 7)
    renderChess('black', '卒', availableLocations, 3, 0)
    renderChess('black', '卒', availableLocations, 3, 2)
    renderChess('black', '卒', availableLocations, 3, 4)
    renderChess('black', '卒', availableLocations, 3, 6)
    renderChess('black', '卒', availableLocations, 3, 8)
    renderChess('black', '車', availableLocations, 0, 0)
    renderChess('black', '車', availableLocations, 0, 8)
    renderChess('black', '馬', availableLocations, 0, 1)
    renderChess('black', '馬', availableLocations, 0, 7)
    renderChess('black', '象', availableLocations, 0, 2)
    renderChess('black', '象', availableLocations, 0, 6)
    renderChess('black', '士', availableLocations, 0, 3)
    renderChess('black', '士', availableLocations, 0, 5)
    renderChess('black', '將', availableLocations, 0, 4)
}

// all coordinates available for chess
var availableLocations = getCoordinates(cellList)

// render a chess, args -- faction: black or red; division: '炮', '兵', etc, location: e.g. availableLocations, x&y: index of location
var renderChess = (faction, division, location, x, y) => {
    var chess = document.createElement('div');
    chess.className = `chessPieces ${faction}`;
    var innerDiv = document.createElement('div');
    var spanElement = document.createElement('span');
    spanElement.textContent = division;
    innerDiv.appendChild(spanElement);
    chess.id = JSON.stringify({ x: x, y: y })
    chess.appendChild(innerDiv);
    chess.style.position = "absolute"
    chess.style.left = location[x][y].x + 47.5;
    chess.style.top = location[x][y].y + 65;
    chess.addEventListener("click", () => {
        try {
            curr_loc = JSON.parse(chess.id)
            available_loc = [{ x: curr_loc.x + 1, y: curr_loc.y + 1 }, { x: curr_loc.x + 0, y: curr_loc.y + 1 }]
            available_loc.map((item) => {
                var availableLocation = document.createElement('div');
                availableLocation.className = `chessPieces ${faction}`;
                availableLocation.style.position = "absolute"
                availableLocation.style.left = location[item.x][item.y].x + 47.5;
                availableLocation.style.top = location[item.x][item.y].y + 65;
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
    board.appendChild(chess);
}

initiate(availableLocations)



