import Pokemon from "./Pokemon";
import { Draggable, Droppable } from "@hello-pangea/dnd";
import clsx from "clsx";
import { MINION_LENGTH } from "../constants";

export default function PlayerBoard({
  isDragDisabled,
  player,
  isDropDisabled,
  isCombineEnabled,
  isShiftDisabled,
}) {
  function getListStyle(isDraggingOver, itemsLength) {
    return {
      // background: isDraggingOver ? "lightblue" : "lightgrey",
      // display: "flex",
      // padding: grid,
      width: Math.max(MINION_LENGTH * itemsLength, MINION_LENGTH),
    };
  }

  function getItemStyle(style, snapshot) {
    if (!snapshot.isDragging && isShiftDisabled) return {};
    if (!snapshot.isDropAnimating && isShiftDisabled) {
      return style;
    }

    if (isShiftDisabled) {
      return {
        ...style,
        transitionDuration: `0.001s`,
      };
    }

    if (snapshot.draggingOver === "droppable-sell") {
      return {
        ...style,
        opacity: 0.5,
      };
    }

    return style;
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
          className={clsx("row rounded-xl bg-white/50")}
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
                  style={getItemStyle(provided.draggableProps.style, snapshot)}
                >
                  <Pokemon key={pokemon.id} {...pokemon} location="board" />
                </div>
              )}
            </Draggable>
          ))}
          <span
            style={{
              display: isShiftDisabled ? "none" : "inline-block",
            }}
          >
            {provided.placeholder}
          </span>
        </div>
      )}
    </Droppable>
  );
}
