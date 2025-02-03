import Pokemon from "./Pokemon";
import { Draggable, Droppable } from "@hello-pangea/dnd";
import clsx from "clsx";
import { MINION_LENGTH } from "../constants";

export default function PlayerBoard({
  isDragDisabled,
  player,
  isDropDisabled,
  isCombineEnabled,
}) {
  function getListStyle(isDraggingOver, itemsLength) {
    return {
      // background: isDraggingOver ? "lightblue" : "lightgrey",
      // display: "flex",
      // padding: grid,
      width: Math.max(MINION_LENGTH * itemsLength, MINION_LENGTH),
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

      // styles we need to apply on draggables

      ...draggableStyle,
    };
  }

  return (
    <Droppable
      droppableId="droppable-board"
      direction="horizontal"
      isDropDisabled={isDropDisabled}
      isCombineEnabled={isCombineEnabled}
    >
      {(provided, snapshot) => (
        <div
          className={clsx("row")}
          ref={provided.innerRef}
          style={getListStyle(snapshot.isDraggingOver, player.board.length)}
          {...provided.droppableProps}
        >
          {player.board.map((pokemon, index) => (
            <Draggable
              key={pokemon.id}
              draggableId={pokemon.id}
              index={index}
              isDragDisabled={isDragDisabled}
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
                  <Pokemon key={pokemon.id} {...pokemon} location="board" />
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
