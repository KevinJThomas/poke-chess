import Pokemon from "./Pokemon";
import { Draggable, Droppable } from "@hello-pangea/dnd";

export default function TavernRow({ tavern, isDragDisabled }) {
  function getListStyle(isDraggingOver, itemsLength) {
    return {
      // background: isDraggingOver ? "lightblue" : "lightgrey",
      // display: "flex",
      // padding: grid,
      // width: itemsLength * 68.44 + 16,
    };
  }

  const getItems = (count) =>
    Array.from({ length: count }, (v, k) => k).map((k) => ({
      id: `item-${k}-tavern`,
      content: `item ${k}`,
    }));

  const items = getItems(3);

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
    <Droppable droppableId="droppable-tavern" direction="horizontal">
      {(provided, snapshot) => (
        <div
          className="flex h-1/5 w-screen items-center justify-center"
          ref={provided.innerRef}
          style={getListStyle(snapshot.isDraggingOver, items.length)}
          {...provided.droppableProps}
        >
          {tavern.map((pokemon, index) => (
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
                  <Pokemon
                    name={pokemon.name}
                    attack={pokemon.attack}
                    health={pokemon.health}
                    id={pokemon.id}
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
