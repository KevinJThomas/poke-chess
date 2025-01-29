import Pokemon from "./Pokemon";
import { Draggable, Droppable } from "@hello-pangea/dnd";
import clsx from "clsx";

export default function HandRow({ hand, isDragDisabled, isDropDisabled }) {
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

      // styles we need to apply on draggables
      ...draggableStyle,
    };
  }

  return (
    <Droppable
      droppableId="droppable-hand"
      direction="horizontal"
      isDropDisabled={isDropDisabled}
    >
      {(provided, snapshot) => (
        <div
          className="row w-[800px]"
          ref={provided.innerRef}
          style={getListStyle(snapshot.isDraggingOver, hand.length)}
          {...provided.droppableProps}
        >
          {hand.map((card, index) => (
            <Draggable
              key={card.id}
              draggableId={card.id}
              index={index}
              isDragDisabled={isDragDisabled}
            >
              {(provided, snapshot) => (
                <div
                  key={card.id}
                  ref={provided.innerRef}
                  {...provided.draggableProps}
                  {...provided.dragHandleProps}
                  style={getItemStyle(
                    snapshot.isDragging,
                    provided.draggableProps.style,
                  )}
                >
                  <Pokemon {...card} />
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
