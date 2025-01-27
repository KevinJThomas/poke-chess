import Pokemon from "./Pokemon";
import { Draggable, Droppable } from "@hello-pangea/dnd";
import clsx from "clsx";

export default function PlayerPokemon({
  isDragDisabled,
  player,
  isDropDisabled,
  board,
}) {
  function getListStyle(isDraggingOver, itemsLength) {
    return {
      // background: isDraggingOver ? "lightblue" : "lightgrey",
      // display: "flex",
      // padding: grid,
      // width: itemsLength * 68.44 + 16,
    };
  }

  function getItemStyle(isDragging, draggableStyle) {
    return {
      // some basic styles to make the items look a bit nicer
      userSelect: "none",
      // padding: grid * 2,
      // margin: `0 0 ${grid}px 0`,

      // change background colour if dragging
      // background: isDragging ? "lightgreen" : "red",

      paddingLeft: 6,
      paddingRight: 6,

      // styles we need to apply on draggables
      ...draggableStyle,
    };
  }

  return (
    <Droppable
      droppableId="droppable-board"
      direction="horizontal"
      isDropDisabled={isDropDisabled}
    >
      {(provided, snapshot) => (
        <div
          className={clsx("flex h-1/5 w-screen items-center justify-center")}
          ref={provided.innerRef}
          style={getListStyle(snapshot.isDraggingOver, player.board.length)}
          {...provided.droppableProps}
        >
          {board.map((pokemon, index) => (
            <Draggable
              key={pokemon.id}
              draggableId={pokemon.id}
              index={index}
              isDragDisabled={isDragDisabled || pokemon.id === "empty-slot"}
            >
              {(provided, snapshot) => (
                <div
                  ref={provided.innerRef}
                  {...provided.draggableProps}
                  {...provided.dragHandleProps}
                  style={getItemStyle(
                    snapshot.isDragging,
                    provided.draggableProps.style,
                  )}
                >
                  <Pokemon key={pokemon.id} {...pokemon} />
                </div>
              )}
            </Draggable>
          ))}
          {provided.placeholder}
        </div>
      )}
    </Droppable>
  );
}
