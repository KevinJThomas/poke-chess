import Hero from "./Hero";
import { Droppable } from "@hello-pangea/dnd";

export default function Shop() {
  function getListStyle(isDraggingOver) {
    return {
      // background: isDraggingOver ? "lightblue" : "lightgrey",
      // display: "flex",
      // padding: grid,
      // width: itemsLength * 68.44 + 16,
    };
  }

  return (
    <Droppable droppableId="droppable-shop" direction="horizontal">
      {(provided, snapshot) => (
        <div
          ref={provided.innerRef}
          style={getListStyle(snapshot.isDraggingOver)}
          {...provided.droppableProps}
        >
          <Hero name="Shop" tier={1} />
        </div>
      )}
    </Droppable>
  );
}
