import Pokemon from "./Pokemon";
import { Draggable, Droppable } from "@hello-pangea/dnd";
import clsx from "clsx";
import { MINION_LENGTH } from "../constants";

export default function TavernRow({ isDragDisabled, player, isDropDisabled }) {
  function getListStyle(isDraggingOver, itemsLength) {
    return {
      // background: isDraggingOver ? "lightblue" : "lightgrey",
      // display: "flex",
      // padding: grid,
      // width: itemsLength * 68.44 + 16,
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
      droppableId="droppable-shop"
      direction="horizontal"
      isDropDisabled={isDropDisabled}
    >
      {(provided, snapshot) => (
        <div
          className={clsx("row", player.isShopFrozen && "bg-blue-200")}
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
                  <Pokemon
                    key={pokemon.id}
                    {...pokemon}
                    showTier
                    location="shop"
                  />
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
