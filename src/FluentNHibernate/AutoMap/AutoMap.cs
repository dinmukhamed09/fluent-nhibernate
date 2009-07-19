using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using FluentNHibernate.Mapping;
using FluentNHibernate.Mapping.Providers;
using FluentNHibernate.MappingModel.ClassBased;
using FluentNHibernate.Utils;

namespace FluentNHibernate.AutoMap
{
    public class AutoMap<T> : ClassMap<T>, IAutoClasslike
    {
        private readonly IList<string> mappedProperties;

        public AutoMap(IList<string> mappedProperties)
        {
            this.mappedProperties = mappedProperties;
        }

        public IEnumerable<string> PropertiesMapped
        {
            get { return mappedProperties; }
        }

        void IAutoClasslike.DiscriminateSubClassesOnColumn(string column)
        {
            DiscriminateSubClassesOnColumn(column);
        }

        void IAutoClasslike.AlterModel(ClassMappingBase classMapping)
        {
            if (classMapping is ClassMapping)
            {
                if (id != null)
                    ((ClassMapping)classMapping).Id = id.GetIdentityMapping();

                if (version != null)
                    ((ClassMapping)classMapping).Version = version.GetVersionMapping();
            }

            foreach (var property in Properties)
                classMapping.AddOrReplaceProperty(property.GetPropertyMapping());

            foreach (var collection in collections)
                classMapping.AddOrReplaceCollection(collection.GetCollectionMapping());

            foreach (var component in Components)
                classMapping.AddOrReplaceComponent(component.GetComponentMapping());

            foreach (var oneToOne in oneToOnes)
                classMapping.AddOrReplaceOneToOne(oneToOne.GetOneToOneMapping());

            foreach (var reference in references)
                classMapping.AddOrReplaceReference(reference.GetManyToOneMapping());

            foreach (var any in anys)
                classMapping.AddOrReplaceAny(any.GetAnyMapping());

            // TODO: Add other mappings
        }

        protected override OneToManyPart<TChild> HasMany<TChild>(PropertyInfo property)
        {
            mappedProperties.Add(property.Name);
            return base.HasMany<TChild>(property);
        }

        public void IgnoreProperty(Expression<Func<T, object>> expression)
        {
            mappedProperties.Add(ReflectionHelper.GetProperty(expression).Name);
        }

        public override IdentityPart Id(Expression<Func<T, object>> expression)
        {
            mappedProperties.Add(ReflectionHelper.GetProperty(expression).Name);
            return base.Id(expression);
        }

        protected override PropertyMap Map(PropertyInfo property, string columnName)
        {
            mappedProperties.Add(property.Name);
            return base.Map(property, columnName);
        }

        protected override ManyToOnePart<TOther> References<TOther>(PropertyInfo property, string columnName)
        {
            mappedProperties.Add(property.Name);
            return base.References<TOther>(property, columnName);
        }

        protected override ManyToManyPart<TChild> HasManyToMany<TChild>(PropertyInfo property)
        {
            mappedProperties.Add(property.Name);
            return base.HasManyToMany<TChild>(property);
        }

        protected override ComponentPart<TComponent> Component<TComponent>(PropertyInfo property, Action<ComponentPart<TComponent>> action)
        {
            mappedProperties.Add(property.Name);

            if (action == null)
                action = c => { };

            return base.Component(property, action);
        }

        public override IdentityPart Id(Expression<Func<T, object>> expression, string column)
        {
            mappedProperties.Add(ReflectionHelper.GetProperty(expression).Name);
            return base.Id(expression, column);
        }

        protected override OneToOnePart<TOther> HasOne<TOther>(PropertyInfo property)
        {
            mappedProperties.Add(property.Name);
            return base.HasOne<TOther>(property);
        }

        protected override VersionPart Version(PropertyInfo property)
        {
            mappedProperties.Add(property.Name);
            return base.Version(property);
        }

		public AutoJoinedSubClassPart<TSubclass> JoinedSubClass<TSubclass>(string keyColumn, Action<AutoJoinedSubClassPart<TSubclass>> action)
			where TSubclass : T
        {
            var genericType = typeof(AutoJoinedSubClassPart<>).MakeGenericType(typeof(TSubclass));
            var joinedclass = (AutoJoinedSubClassPart<TSubclass>)Activator.CreateInstance(genericType, keyColumn);

            action(joinedclass);

            joinedSubclasses[typeof(TSubclass)] = joinedclass;

		    return joinedclass;
        }

        public IAutoClasslike JoinedSubClass(Type type, string keyColumn)
        {
            var genericType = typeof (AutoJoinedSubClassPart<>).MakeGenericType(type);
            var joinedclass = (IJoinedSubclassMappingProvider)Activator.CreateInstance(genericType, keyColumn);

            // remove any mappings for the same type, then re-add
            joinedSubclasses[type] = joinedclass;

            return (IAutoClasslike)joinedclass;
        }

        public AutoJoinedSubClassPart<TSubclass> JoinedSubClass<TSubclass>(string keyColumn)
			where TSubclass : T
		{
			return JoinedSubClass<TSubclass>(keyColumn, null);
		}

		public AutoSubClassPart<TSubclass> SubClass<TSubclass>(object discriminatorValue, Action<AutoSubClassPart<TSubclass>> action)
			where TSubclass : T
        {
            var genericType = typeof(AutoSubClassPart<>).MakeGenericType(typeof(TSubclass));
            var subclass = (AutoSubClassPart<TSubclass>)Activator.CreateInstance(genericType, null, discriminatorValue);
            
            action(subclass);

            // remove any mappings for the same type, then re-add
            subclasses[typeof(TSubclass)] = subclass;

		    return subclass;
        }

        public AutoSubClassPart<TSubclass> SubClass<TSubclass>(object discriminatorValue)
			where TSubclass : T
		{
			return SubClass<TSubclass>(discriminatorValue, null);
		}

        public IAutoClasslike SubClass(Type type, string discriminatorValue)
        {
            var genericType = typeof(AutoSubClassPart<>).MakeGenericType(type);
            var subclass = (ISubclassMappingProvider)Activator.CreateInstance(genericType, null, discriminatorValue);

            // remove any mappings for the same type, then re-add
            subclasses[type] = subclass;

            return (IAutoClasslike)subclass;
        }
    }
}