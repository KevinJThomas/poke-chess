import Pokemon from "./Pokemon";
import { Draggable, Droppable } from "@hello-pangea/dnd";
import clsx from "clsx";

export default function TavernRow({ isDragDisabled, player, isDropDisabled }) {
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
      droppableId="droppable-shop"
      direction="horizontal"
      isDropDisabled={isDropDisabled}
    >
      {(provided, snapshot) => (
        <div
          className={clsx(
            "flex h-1/5 w-screen items-center justify-center",
            player.isShopFrozen && "bg-blue-200",
          )}
          ref={provided.innerRef}
          style={getListStyle(snapshot.isDraggingOver, player.shop.length)}
          {...provided.droppableProps}
        >
          {(player.shop ?? []).map((pokemon, index) => (
            <Draggable
              key={pokemon.id}
              draggableId={pokemon.id}
              index={index}
              isDragDisabled={isDragDisabled || pokemon.cost > player.gold}
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
